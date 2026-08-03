# Hướng dẫn Deploy MqSocial (ASP.NET Zero + Angular + PostgreSQL) trên Ubuntu

> Ghi chú: guide này đúc kết từ lần deploy thực tế lên server `103.162.21.123` / domain `mqsocial.vn`. Khi deploy server mới, thay các giá trị (IP, domain, mật khẩu) cho phù hợp.

**Kiến trúc tổng quan:**

```
mqsocial.vn / www.mqsocial.vn      → Landing page (static, Nginx serve)
manager.mqsocial.vn                → Angular (Nginx static)
                                       + proxy /api, /signalr, route gốc ABP → upstream "mqsocial_api" (Nginx LB, ip_hash)
                                                    ↓                              ↓
                                       MqSocial.Web.Host #1 (:5000)   MqSocial.Web.Host #2 (:5001)
                                                    └──────────────┬───────────────┘
                                                                    ↓
                                                    PostgreSQL (mqsocial_db, localhost:5432)
                                                                    ↑
                                                    MqSocial.Scheduler (Hangfire, systemd — CHỈ 1 instance, không nhân bản)
```

> Từ mục 15: backend chạy 2 instance trên cùng server (chống downtime khi 1 process crash). Trước mục 15, kiến trúc chỉ có 1 instance `MqSocial.Web.Host` port 5000 — xem mục 8.

---

## 0. Yêu cầu trước khi bắt đầu

- Server Ubuntu 24.04 LTS (hoặc tương đương), có quyền root
- Domain đã trỏ nameserver về nhà đăng ký bạn quản lý DNS được
- Project source code (ASP.NET Zero 10.2.0, .NET 9, Angular) build được ở máy local (Windows)
- `scp`/`ssh` sẵn sàng dùng từ máy local

---

## 1. Đổi mật khẩu root (nếu cần)

```bash
sudo passwd root          # đang có quyền sudo
passwd                    # đang đăng nhập root trực tiếp
sudo passwd -l root       # khóa lại đăng nhập root trực tiếp nếu muốn (khuyến nghị)
```

---

## 2. Cài PostgreSQL 18 qua kho PGDG chính thức

```bash
sudo apt update
sudo apt install -y curl ca-certificates
sudo install -d /usr/share/postgresql-common/pgdg
sudo curl -o /usr/share/postgresql-common/pgdg/apt.postgresql.org.asc --fail \
  https://www.postgresql.org/media/keys/ACCC4CF8.asc

sudo apt install -y postgresql-common
sudo /usr/share/postgresql-common/pgdg/apt.postgresql.org.sh

sudo apt update
sudo apt install -y postgresql-18 postgresql-client-18

psql --version
sudo systemctl status postgresql
```

### 2.1. Tạo database + user riêng cho MqSocial (không dùng superuser)

```bash
sudo -u postgres psql
```

```sql
CREATE USER mqsocial_user WITH PASSWORD 'MẬT_KHẨU_MẠNH';
CREATE DATABASE mqsocial_db OWNER mqsocial_user;
GRANT ALL PRIVILEGES ON DATABASE mqsocial_db TO mqsocial_user;
\q
```

> `mqsocial_user` chỉ có toàn quyền trong `mqsocial_db`, không phải superuser. Superuser thật là `postgres`.

### 2.2. Kiểm tra Postgres không mở ra ngoài internet

```bash
sudo ufw status
sudo netstat -tlnp | grep 5432
```

Đảm bảo Postgres chỉ nghe `localhost` (mặc định), không cần mở port 5432 ra ngoài nếu app chạy cùng server.

---

## 3. Cài Nginx

```bash
sudo apt update
sudo apt install -y nginx
sudo systemctl enable --now nginx
sudo ufw allow 'Nginx Full'   # nếu dùng UFW
```

### 3.1. Cấu trúc thư mục web

```bash
mkdir -p /var/www/mqsocial/landing     # landing page domain gốc
mkdir -p /var/www/mqsocial/manager     # Angular build (frontend app)
mkdir -p /var/www/mqsocial/api         # Backend .NET publish output
mkdir -p /var/www/mqsocial/migrator    # Migrator publish output
mkdir -p /var/www/mqsocial/scheduler   # Scheduler (Hangfire) publish output
mkdir -p /var/www/mqsocial/keys        # DataProtection keys (persist login session)
```

---

## 4. Trỏ domain (DNS)

Tại nhà đăng ký domain, thêm các bản ghi A — **tất cả cùng trỏ về IP server**:

| Loại | Host     | Value          |
|------|----------|----------------|
| A    | @        | `<IP_SERVER>`  |
| A    | www      | `<IP_SERVER>`  |
| A    | manager  | `<IP_SERVER>`  |

Kiểm tra đã lan truyền:

```bash
nslookup mqsocial.vn 8.8.8.8
nslookup manager.mqsocial.vn 8.8.8.8
```

> Nếu `nslookup` không dùng DNS server (8.8.8.8) mà bị lỗi "Non-existent domain" trên máy Windows local, thường chỉ do cache DNS local/router — không phải lỗi cấu hình. Luôn test với `8.8.8.8` để biết DNS thật đã đúng chưa.

---

## 5. Cấu hình Nginx cho 2 nhóm domain

### 5.1. Landing page — `/etc/nginx/sites-available/mqsocial-landing`

```nginx
server {
    listen 80;
    server_name mqsocial.vn www.mqsocial.vn;

    root /var/www/mqsocial/landing;
    index index.html;

    location / {
        try_files $uri $uri/ =404;
    }
}
```

### 5.2. Manager app — `/etc/nginx/sites-available/mqsocial-manager`

```nginx
server {
    listen 80;
    server_name manager.mqsocial.vn;

    root /var/www/mqsocial/manager;
    index index.html;

    client_max_body_size 50M;

    # Các route gốc của ASP.NET Zero backend (không theo tiền tố /api)
    location ~ ^/(AbpUserConfiguration|TokenAuth|Account|Abp|connect|Session|AccountVerify|Migration|Notification) {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # API backend
    location /api {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # SignalR (real-time notification)
    location /signalr {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 86400;
    }

    # Angular SPA - fallback về index.html cho client-side routing
    location / {
        try_files $uri $uri/ /index.html;
    }
}
```

### 5.3. Kích hoạt

```bash
ln -s /etc/nginx/sites-available/mqsocial-landing /etc/nginx/sites-enabled/
ln -s /etc/nginx/sites-available/mqsocial-manager /etc/nginx/sites-enabled/
rm -f /etc/nginx/sites-enabled/default
nginx -t
systemctl reload nginx
```

---

## 6. HTTPS miễn phí với Certbot (Let's Encrypt)

```bash
apt install -y certbot python3-certbot-nginx
certbot --nginx -d mqsocial.vn -d www.mqsocial.vn -d manager.mqsocial.vn
```

- Chọn **Redirect HTTP → HTTPS** khi được hỏi.
- Certbot tự thêm `listen 443 ssl` + đường dẫn cert vào từng file config tương ứng.
- Cert Let's Encrypt **miễn phí vĩnh viễn**, hết hạn sau 90 ngày nhưng **tự động gia hạn** qua `certbot.timer`/cron có sẵn — không cần thao tác gì thêm.

> **Đã từng gặp thực tế (03/08):** chạy `certbot --nginx -d mqsocial.vn -d www.mqsocial.vn -d manager.mqsocial.vn` trong 1 lệnh — certbot cấp cert cho cả 3 domain đúng (`certbot certificates` thấy đủ), và sửa nginx đúng cho `mqsocial.vn`/`www.mqsocial.vn`, nhưng với `manager.mqsocial.vn` thì **chỉ tạo được block redirect (`listen 80` → 301, kèm `return 404;` cho host lạ) mà quên hẳn thêm `listen 443 ssl` + cert vào block nội dung chính**. Hậu quả: gọi `https://manager.mqsocial.vn` không khớp `server_name` nào ở cổng 443 → Nginx rơi vào server block 443 đầu tiên trong toàn bộ config làm default (ở đây là landing page) → dính `location / { try_files ... =404; }` của landing page → **404** dù backend hoàn toàn bình thường. Lỗi này im lặng, không có gì trong log Nginx/certbot báo rõ ràng.
>
> **Cách nhận biết:** `curl -i http://localhost:5000/...` (thẳng backend, bỏ qua Nginx) ra `200` nhưng `curl -i https://manager.mqsocial.vn/...` ra `404` → nghi ngay Nginx thiếu block 443 đúng domain, không phải lỗi backend. Xác nhận bằng `nginx -T 2>/dev/null | grep -n "server_name manager.mqsocial.vn\|listen "` — nếu không thấy `listen 443 ssl` nào nằm trong phạm vi block `manager.mqsocial.vn`, đúng là bị lỗi này.
>
> **Cách sửa:** cert vẫn còn hạn (`certbot certificates` xác nhận `Certificate Path` tồn tại) nên không cần cấp lại — chỉ cần thủ công thêm đúng 5 dòng vào block nội dung của domain bị thiếu, đặt cạnh `location /` cuối cùng trước dấu `}` đóng block (bỏ luôn `listen 80;` khỏi block này vì block redirect riêng đã lo phần đó):
> ```nginx
>     listen 443 ssl; # managed by Certbot
>     ssl_certificate /etc/letsencrypt/live/<domain>/fullchain.pem; # managed by Certbot
>     ssl_certificate_key /etc/letsencrypt/live/<domain>/privkey.pem; # managed by Certbot
>     include /etc/letsencrypt/options-ssl-nginx.conf; # managed by Certbot
>     ssl_dhparam /etc/letsencrypt/ssl-dhparams.pem; # managed by Certbot
> ```
> Rồi `nginx -t && systemctl reload nginx`. **Sau khi certbot chạy xong, luôn kiểm tra lại từng domain** bằng lệnh grep ở trên hoặc `curl -i https://<domain>` trước khi coi như xong bước này — đừng tin certbot đã làm đúng hết cho mọi domain chỉ vì lệnh chạy không báo lỗi.

Kiểm tra auto-renew hoạt động:

```bash
systemctl status certbot.timer
certbot renew --dry-run
```

---

## 7. Cài .NET 9 Runtime

Ubuntu 24.04 (noble) không có sẵn .NET 9 trong kho mặc định (chỉ có .NET 8) — cần thêm PPA backports:

```bash
apt install -y software-properties-common
add-apt-repository -y ppa:dotnet/backports
apt update
apt install -y aspnetcore-runtime-9.0

dotnet --list-runtimes
which dotnet     # phải ra /usr/bin/dotnet
```

> Nếu project dùng .NET 8 (LTS, khuyến nghị cho production lâu dài), bỏ qua bước thêm PPA, chỉ cần: `apt install -y aspnetcore-runtime-8.0`

---

## 8. Deploy Backend (MqSocial.Web.Host)

### 8.1. Build ở máy local

```powershell
cd aspnet-core\src\MqSocial.Web.Host
dotnet publish -c Release -o publish
```

### 8.2. Tạo `appsettings.Production.json`

Trong **project gốc** (không chỉ trong `publish/`), tạo file:
`aspnet-core\src\MqSocial.Web.Host\appsettings.Production.json`

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=mqsocial_db;Username=mqsocial_user;Password=MẬT_KHẨU"
  },
  "App": {
    "ServerRootAddress": "https://manager.mqsocial.vn/",
    "ClientRootAddress": "https://manager.mqsocial.vn/",
    "CorsOrigins": "https://manager.mqsocial.vn,https://mqsocial.vn"
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      }
    }
  }
}
```

> **Quan trọng:** phần `Kestrel.Endpoints.Http` bắt buộc phải có — nếu `appsettings.json` gốc có định nghĩa `Https` endpoint (cần cert riêng cho Kestrel) thì service sẽ crash lúc start vì không tìm thấy chứng chỉ. Vì HTTPS đã do Nginx đảm nhiệm (SSL termination), backend chỉ cần chạy HTTP nội bộ.

> **Đã từng gặp thực tế (19/07):** `appsettings.json` gốc (dùng cho dev local) có endpoint đặt tên là `"Http"` nhưng `Url` lại là `https://localhost:44311/`. Kestrel bind endpoint theo **scheme trong URL, không theo tên key** — nên dù tên là "Http", Kestrel vẫn hiểu đây là HTTPS và đòi cert. Nếu `appsettings.Production.json` **mất khối `Kestrel.Endpoints.Http`** (ví dụ bị ghi đè/tạo lại khi sửa code mà quên giữ lại đoạn này), service sẽ crash liên tục dạng core-dump với lỗi:
> ```
> System.InvalidOperationException: Unable to configure HTTPS endpoint. No server certificate was specified...
> ```
> Cách nhận biết: `journalctl -u mqsocial-api -f` thấy lỗi trên + service tự restart lặp vô hạn (`Scheduled restart job, restart counter is at N`). Cách sửa: kiểm tra lại `aspnet-core\src\MqSocial.Web.Host\appsettings.Production.json` ở **local** có đủ khối `Kestrel.Endpoints.Http.Url = "http://localhost:5000"` như mẫu trên chưa, publish + `scp` đè lại, rồi `systemctl restart mqsocial-api`.

Kiểm tra `.csproj` có copy file `appsettings.Production.json` khi publish (thường mặc định copy theo wildcard `appsettings*.json`, không cần sửa gì thêm).

Build lại và upload:

```powershell
dotnet publish -c Release -o publish
scp -r publish\* root@103.162.21.123:/var/www/mqsocial/api/
```

### 8.3. Kiểm tra file `log4net.config` có đi kèm không

ASP.NET Zero dùng Castle Windsor + log4net, cần file `log4net.config` cạnh file `.dll` khi chạy, nếu không sẽ lỗi `FileNotFoundException`.

```bash
ls /var/www/mqsocial/api/log4net.config
```

Nếu thiếu, copy từ project gốc:
```powershell
scp aspnet-core\src\MqSocial.Web.Host\log4net.config root@<IP_SERVER>:/var/www/mqsocial/api/
```

### 8.4. Tạo systemd service

```bash
nano /etc/systemd/system/mqsocial-api.service
```

```ini
[Unit]
Description=MqSocial Web.Host API
After=network.target postgresql.service

[Service]
WorkingDirectory=/var/www/mqsocial/api
ExecStart=/usr/bin/dotnet /var/www/mqsocial/api/MqSocial.Web.Host.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=mqsocial-api
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

```bash
chown -R www-data:www-data /var/www/mqsocial/api
systemctl daemon-reload
systemctl enable --now mqsocial-api
systemctl status mqsocial-api
journalctl -u mqsocial-api -f
```

### 8.5. Test

```bash
curl -i http://localhost:5000/api/services/app/Session/GetCurrentLoginInformations
curl -i https://manager.mqsocial.vn/AbpUserConfiguration/GetAll
```

Cả 2 phải trả về `200 OK` với JSON.

---

## 9. Chạy Migrator (tạo schema + seed data)

### 9.1. Build và upload

```powershell
cd aspnet-core\src\MqSocial.Migrator
dotnet publish -c Release -o publish
scp -r publish\* root@103.162.21.123:/var/www/mqsocial/migrator/
```

Đảm bảo `appsettings.json` của Migrator cũng trỏ đúng connection string Postgres, và có `log4net.config` đi kèm (xem mục 8.3).

### 9.2. Chạy

```bash
cd /var/www/mqsocial/migrator
dotnet MqSocial.Migrator.dll
```

Xác nhận:
```bash
sudo -u postgres psql -d mqsocial_db -c "\dt"
```

> **Nếu code còn dùng SQL Server thay vì Postgres:** phải đổi package (`Npgsql.EntityFrameworkCore.PostgreSQL` thay `...SqlServer`), đổi `UseNpgsql()` thay `UseSqlServer()` trong `DbContextConfigurer`, xóa migrations cũ và `dotnet ef migrations add` lại — vì migration sinh cho SQL Server không tương thích Postgres.
>
> **Lỗi DateTime thường gặp với Npgsql:** nếu thấy `Cannot write DateTime with Kind=Unspecified/Local`, thêm dòng sau vào đầu `Main()` của cả Migrator, Web.Host, và Scheduler:
> ```csharp
> AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
> ```

---

## 10. Deploy Frontend (Angular)

### 10.1. Sửa config production

`angular\src\assets\appconfig.production.json`:

```json
{
  "remoteServiceBaseUrl": "https://manager.mqsocial.vn",
  "appBaseUrl": "https://manager.mqsocial.vn",
  "localeMappings": [],
  "googleMapsApiKey": ""
}
```

### 10.2. Build

```powershell <DEPLOY FE>
cd angular
npm install 
npx ng build --configuration production
```

> ASP.NET Zero Angular 10.2.0 (Angular 17+) không có sẵn script `npm run publish` — dùng thẳng Angular CLI. Output nằm ở `dist\browser\` (không phải `dist\` phẳng).

### 10.3. Upload

```bash
# trên server - dọn thư mục cũ trước
rm -rf /var/www/mqsocial/manager/*
```

```powershell
# trên local
scp -r dist\browser\* root@103.162.21.123:/var/www/mqsocial/manager/
```

```bash
chown -R www-data:www-data /var/www/mqsocial/manager
```

### 10.4. Test

Mở `https://manager.mqsocial.vn` — phải thấy màn hình đăng nhập ASP.NET Zero.

---

## 11. Persist DataProtection Keys (tránh mất session khi restart)

Mặc định, nếu chạy dưới user không có profile cố định (như `www-data`), key sẽ lưu tạm trong RAM → mất session mỗi lần restart service.

### 11.1. Sửa code (`Startup.cs` của Web.Host)

Trong `ConfigureServices()`:

```csharp
services.AddDataProtection()
    .SetApplicationName("MqSocial")
    .PersistKeysToFileSystem(new DirectoryInfo("/var/www/mqsocial/keys"));
```

Thêm `using System.IO;` nếu chưa có.

### 11.2. Tạo thư mục và build lại

```bash
mkdir -p /var/www/mqsocial/keys
chown -R www-data:www-data /var/www/mqsocial/keys
chmod 700 /var/www/mqsocial/keys
```

```powershell
dotnet publish -c Release -o publish
scp -r publish\* root@<IP_SERVER>:/var/www/mqsocial/api/
```

```bash
systemctl restart mqsocial-api
ls -la /var/www/mqsocial/keys/     # phải thấy file key-xxxx.xml
```

---

## 12. Deploy MqScheduler (Hangfire, console app — project `schedule\`)

> Project thật nằm ở **`schedule\`** (top-level, ngang hàng `aspnet-core\`), không phải `aspnet-core\src\MqSocial.Scheduler`. Target `net9.0`, framework-dependent (không self-contained) — dùng chung runtime .NET 9 đã cài cho `mqsocial-api`, không cần cài thêm gì trên server.
>
> **Đã từng gặp thực tế (22-23/07):** project ban đầu tạo với `TargetFramework=net10.0` (kèm `Microsoft.Extensions.Hosting` bản 10.x) trong khi cả máy dev lẫn server chỉ có .NET 9 — publish local báo `NETSDK1045: The current .NET SDK does not support targeting .NET 10.0`, còn nếu build ở máy khác rồi deploy thì server chạy báo `You must install or update .NET to run this application... Framework: 'Microsoft.NETCore.App', version '10.0.0'`. Đã đổi hẳn về `net9.0` (và hạ `Microsoft.Extensions.Hosting` xuống `9.0.0`) để khớp với phần còn lại của hệ thống, tránh phải cài song song 2 runtime trên server.

### 12.1. Build

Thư mục `schedule\` có cả `.csproj`, `.sln`, `.slnx` cùng lúc → `dotnet publish` không tự chọn được, phải chỉ định rõ file `.csproj`:

```powershell
cd schedule
dotnet publish schedule.csproj -c Release -o publish
```

### 12.2. Kiểm tra connection string trong `publish\appsettings.json`

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=mqsocial_db;Username=mqsocial_user;Password=MẬT_KHẨU"
  }
}
```

> **Quan trọng:** `appsettings.json` ở local thường trỏ DB dev (user/password khác production). Kiểm tra lại file **sau khi build**, trước khi upload — không copy đè thẳng connection string dev lên server.

### 12.3. Upload

```bash
mkdir -p /var/www/mqsocial/scheduler
```

```powershell
scp -r publish\* root@103.162.21.123:/var/www/mqsocial/scheduler/
```

### 12.4. Test chạy tay trước

```bash
cd /var/www/mqsocial/scheduler
chmod +x ./schedule
./schedule
```

Kỳ vọng: Hangfire khởi động, đăng ký recurring jobs (`update-contract-kol-result`, `update-kol-source`...) không lỗi. `Ctrl+C` để dừng.

> Đảm bảo project dùng `Hangfire.PostgreSql` (không phải `Hangfire.SqlServer`).
> Kiểm tra `Web.Host` không đồng thời gọi `app.UseHangfireServer()` — nếu có, sẽ chạy trùng 2 Hangfire server cùng lúc, gây race-condition job.

### 12.5. Tạo systemd service

```bash
nano /etc/systemd/system/mqsocial-scheduler.service
```

```ini
[Unit]
Description=MqSocial Scheduler (Hangfire)
After=network.target postgresql.service mqsocial-api.service

[Service]
WorkingDirectory=/var/www/mqsocial/scheduler
ExecStart=/usr/bin/dotnet /var/www/mqsocial/scheduler/schedule.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=mqsocial-scheduler
User=www-data
Environment=DOTNET_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

> **Đã từng gặp thực tế (23/07):** dùng `ExecStart=/var/www/mqsocial/scheduler/schedule` (gọi thẳng apphost binary) khiến service liên tục restart-loop với lỗi `You must install or update .NET to run this application... No frameworks were found`, dù chạy tay qua SSH (`./schedule` hoặc `dotnet --list-runtimes`) vẫn ra bình thường — apphost tự dò `dotnet` không ổn định trong PATH tối giản của systemd. Gọi qua muxer (`/usr/bin/dotnet <path>.dll`) như trên — giống cách `mqsocial-api` đang chạy — thì hết lỗi.

```bash
chown -R www-data:www-data /var/www/mqsocial/scheduler
systemctl daemon-reload
systemctl enable --now mqsocial-scheduler
systemctl status mqsocial-scheduler
journalctl -u mqsocial-scheduler -f
```

> Thoát `journalctl -f` (`Ctrl+C`) chỉ đóng cửa sổ xem log, **không** dừng service — service vẫn chạy nền qua systemd.

### 12.6. Xác nhận job đã đăng ký

```bash
sudo -u postgres psql -d mqsocial_db -c "\dt \"HangFire\".*"
sudo -u postgres psql -d mqsocial_db -c "SELECT id, cron FROM \"HangFire\".recurringjob;"
```

### 12.7. Redeploy (cập nhật code sau này)

```powershell
cd schedule
dotnet publish schedule.csproj -c Release -o publish
```

```bash
systemctl stop mqsocial-scheduler     # tránh 2 instance tranh job khi ghi đè file
```

```powershell
scp -r publish\* root@103.162.21.123:/var/www/mqsocial/scheduler/
```

```bash
cat /var/www/mqsocial/scheduler/appsettings.json    # kiểm tra lại connection string, phòng bị ghi đè
chown -R www-data:www-data /var/www/mqsocial/scheduler
systemctl start mqsocial-scheduler
journalctl -u mqsocial-scheduler -f
```

> `jobManager.AddOrUpdate(...)` trong `Program.cs` là idempotent — mỗi lần service start sẽ tự cập nhật lại lịch cron nếu có đổi trong code, không cần thao tác gì thêm trong DB.

---

## 13. Backup PostgreSQL tự động hàng ngày

### 13.1. Tạo thư mục backup

```bash
mkdir -p /var/backups/postgresql
chown postgres:postgres /var/backups/postgresql
chmod 700 /var/backups/postgresql
touch /var/log/mqsocial-backup.log
chown postgres:postgres /var/log/mqsocial-backup.log
```

### 13.2. Script backup — `/usr/local/bin/backup-mqsocial-db.sh`

```bash
#!/bin/bash
cd /var/backups/postgresql || exit 1

DB_NAME="mqsocial_db"
DB_USER="postgres"
BACKUP_DIR="/var/backups/postgresql"
DATE=$(date +%Y-%m-%d_%H-%M-%S)
BACKUP_FILE="$BACKUP_DIR/${DB_NAME}_${DATE}.sql.gz"
RETENTION_DAYS=3

pg_dump -U "$DB_USER" -F c "$DB_NAME" | gzip > "$BACKUP_FILE"

if [ $? -eq 0 ]; then
    echo "$(date): Backup thành công -> $BACKUP_FILE" >> /var/log/mqsocial-backup.log
else
    echo "$(date): Backup THẤT BẠI" >> /var/log/mqsocial-backup.log
fi

find "$BACKUP_DIR" -name "${DB_NAME}_*.sql.gz" -mtime +$RETENTION_DAYS -delete
```

```bash
chmod +x /usr/local/bin/backup-mqsocial-db.sh
sudo -u postgres /usr/local/bin/backup-mqsocial-db.sh    # test chạy tay
ls -lh /var/backups/postgresql/
cat /var/log/mqsocial-backup.log
```

### 13.3. Thêm crontab — chạy mỗi ngày 2h sáng

```bash
crontab -u postgres -e
```

Thêm dòng:
```
0 2 * * * /usr/local/bin/backup-mqsocial-db.sh
```

```bash
crontab -u postgres -l          # xác nhận
systemctl status cron           # đảm bảo cron service đang chạy
```

> Retention hiện tại: chỉ giữ **3 ngày** gần nhất, file cũ hơn tự động bị xóa mỗi lần script chạy.

### 13.4. Restore khi cần

```bash
gunzip -c /var/backups/postgresql/mqsocial_db_YYYY-MM-DD_HH-MM-SS.sql.gz \
  | sudo -u postgres pg_restore -d mqsocial_db --clean
```

---

## 14. Checklist kiểm tra tổng thể sau khi deploy xong

```bash
systemctl status postgresql
systemctl status nginx
systemctl status mqsocial-api
systemctl status mqsocial-scheduler
systemctl status cron
certbot certificates
crontab -u postgres -l
```

```bash
curl -i https://mqsocial.vn
curl -i https://manager.mqsocial.vn
curl -i https://manager.mqsocial.vn/api/services/app/Session/GetCurrentLoginInformations
```

---

## 15. Load Balancing nhiều instance backend trên cùng 1 server (chống downtime khi 1 process crash)

> **Phạm vi mục này:** bảo vệ khỏi trường hợp 1 process `MqSocial.Web.Host` bị crash/treo (OOM, lỗi runtime, đứng lúc deploy...) — request của user tự động được chuyển sang instance còn sống, gần như không nhận ra downtime.
>
> **Không** chống được trường hợp cả VPS chết (hỏng ổ đĩa, mất mạng, sập DC/hosting) — trường hợp đó cần server thứ 2 hoàn toàn độc lập + load balancer riêng + Postgres HA/replication (hiện Postgres vẫn là 1 điểm chết duy nhất dù backend có bao nhiêu instance) + DataProtection keys/SignalR dùng chung qua storage ngoài server. Phức tạp và tốn kém hơn hẳn — chỉ nên làm khi thực sự cần chống được mất cả server.

### 15.1. Vì sao cách này work

- Chạy **2 process `MqSocial.Web.Host` độc lập** trên cùng VPS, khác port (`5000`, `5001`).
- Nginx đóng vai trò load balancer qua `upstream`: dàn tải + tự động ngừng gửi request tới instance đang lỗi (passive health check có sẵn trong Nginx open-source, không cần Nginx Plus).
- Cả 2 instance dùng chung:
  - **1 database Postgres** — đã đúng sẵn, không cần đổi gì.
  - **1 thư mục DataProtection keys** `/var/www/mqsocial/keys` (đã setup ở mục 11) — **bắt buộc phải có trước khi làm mục này**. Nếu bỏ qua, mỗi instance tự sinh key riêng: user login ở instance A, request kế tiếp bị Nginx route sang instance B sẽ bị văng ra ngoài vì cookie/token không giải mã được.
- **`mqsocial-scheduler` (Hangfire) giữ nguyên đúng 1 instance** — tuyệt đối không nhân bản theo cách dưới đây. Đã cảnh báo ở mục 12.4: 2 Hangfire server chạy trùng gây race-condition job (chạy lặp/đụng nhau).

### 15.2. Gỡ service đơn cũ, tạo systemd template cho nhiều instance

Service đơn `mqsocial-api.service` đang hardcode port 5000 qua `appsettings.Production.json`. Đổi qua **systemd template** để chạy N instance từ 1 file cấu hình duy nhất, port truyền qua biến môi trường.

```bash
systemctl stop mqsocial-api
systemctl disable mqsocial-api
rm /etc/systemd/system/mqsocial-api.service
systemctl daemon-reload
```

Tạo file template (chú ý tên có `@` trước `.service`):

```bash
nano /etc/systemd/system/mqsocial-api@.service
```

```ini
[Unit]
Description=MqSocial Web.Host API (instance %i)
After=network.target postgresql.service

[Service]
WorkingDirectory=/var/www/mqsocial/api
ExecStart=/usr/bin/dotnet /var/www/mqsocial/api/MqSocial.Web.Host.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=mqsocial-api-%i
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=Kestrel__Endpoints__Http__Url=http://localhost:%i
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

> **Vì sao dùng `Kestrel__Endpoints__Http__Url` chứ không phải `ASPNETCORE_URLS`:** `appsettings.Production.json` đang khai báo cứng `Kestrel:Endpoints:Http:Url` (xem mục 8.2). Khi Kestrel đã có cấu hình endpoint kiểu này, nó **ưu tiên hơn** biến `ASPNETCORE_URLS`/`--urls` — set `ASPNETCORE_URLS=http://localhost:5001` sẽ bị bỏ qua hoàn toàn, cả 2 instance vẫn cùng cố bind port 5000 và 1 trong 2 sẽ crash lúc start vì trùng port. Phải override đúng key phân cấp (`Kestrel__Endpoints__Http__Url`, 2 dấu gạch dưới = 1 cấp con trong JSON) thì biến môi trường mới thắng được giá trị đã khai báo trong file JSON.

`%i` trong file template sẽ được thay bằng phần sau dấu `@` lúc start service — dùng luôn số port cho dễ nhớ:

```bash
chown -R www-data:www-data /var/www/mqsocial/api
systemctl daemon-reload
systemctl enable --now mqsocial-api@5000
systemctl enable --now mqsocial-api@5001
systemctl status mqsocial-api@5000 mqsocial-api@5001
journalctl -u mqsocial-api@5000 -u mqsocial-api@5001 -f
```

Muốn thêm instance thứ 3 (nếu VPS còn dư CPU core): `systemctl enable --now mqsocial-api@5002`, rồi thêm 1 dòng `server` tương ứng vào `upstream` ở bước 15.3 — không cần sửa gì khác.

### 15.3. Cấu hình Nginx upstream

Sửa `/etc/nginx/sites-available/mqsocial-manager`: thêm khối `upstream` ở **ngoài** block `server {}`, đổi mọi `proxy_pass http://localhost:5000;` thành `proxy_pass http://mqsocial_api;`:

```nginx
upstream mqsocial_api {
    server 127.0.0.1:5000 max_fails=3 fail_timeout=10s;
    server 127.0.0.1:5001 max_fails=3 fail_timeout=10s;
}

server {
    server_name manager.mqsocial.vn;

    root /var/www/mqsocial/manager;
    index index.html;

    client_max_body_size 50M;

    location ~ ^/(AbpUserConfiguration|TokenAuth|Account|Abp|connect|Session|AccountVerify|Migration|Notification) {
        proxy_pass http://mqsocial_api;
        proxy_next_upstream error timeout invalid_header http_500 http_502 http_503;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location /api {
        proxy_pass http://mqsocial_api;
        proxy_next_upstream error timeout invalid_header http_500 http_502 http_503;
        proxy_http_version 1.1;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location /signalr {
        proxy_pass http://mqsocial_api;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_read_timeout 86400;
    }

    location / {
        try_files $uri $uri/ /index.html;
    }

    listen 443 ssl; # managed by Certbot
    ssl_certificate /etc/letsencrypt/live/manager.mqsocial.vn/fullchain.pem; # managed by Certbot
    ssl_certificate_key /etc/letsencrypt/live/manager.mqsocial.vn/privkey.pem; # managed by Certbot
    include /etc/letsencrypt/options-ssl-nginx.conf; # managed by Certbot
    ssl_dhparam /etc/letsencrypt/ssl-dhparams.pem; # managed by Certbot
}
server {
    if ($host = manager.mqsocial.vn) {
        return 301 https://$host$request_uri;
    } # managed by Certbot
    listen 80;
    server_name manager.mqsocial.vn;
    return 404; # managed by Certbot
}
```

> **Không dùng `ip_hash`** — round-robin (mặc định của Nginx khi không khai báo `ip_hash`/`least_conn`/`hash`) chia request đều cho cả 2 instance. Chỉ an toàn làm vậy vì DataProtection keys đã dùng chung qua `/var/www/mqsocial/keys` (mục 11) — nhờ đó cookie/token đăng nhập giải mã được ở cả 2 instance, 2 request liên tiếp của cùng 1 user rơi vào 2 instance khác nhau vẫn hợp lệ. **Nếu chưa xác nhận cả 2 instance đọc/ghi được thư mục `keys` (không còn `UnauthorizedAccessException` trong `journalctl`) thì đừng bỏ `ip_hash`** — thiếu điều kiện đó, round-robin sẽ làm user bị văng đăng nhập ngẫu nhiên thường xuyên.
>
> `proxy_next_upstream ... http_502 http_503` giúp Nginx **tự retry sang instance còn sống ngay trong cùng 1 request** nếu instance đầu tiên trả lỗi/timeout — đây là phần tạo cảm giác "không downtime" cho user, không chỉ dừng ở việc ngừng route tới instance chết cho các request *sau đó*. Không thêm dòng này vào `location /signalr` vì SignalR là kết nối dài hơi (WebSocket) — retry giữa chừng không có ý nghĩa, client sẽ tự reconnect qua instance khác nếu rớt kết nối. Đổi round-robin/`ip_hash` cũng không ảnh hưởng 1 connection SignalR đang chạy — kết nối đã mở luôn dính nguyên vào 1 instance suốt thời gian sống của nó, thuật toán chỉ quyết định instance nào nhận connection *mới*.
>
> Certbot đã thêm `listen 443 ssl` + đường dẫn cert vào block này ở mục 6 (và tách 1 block `listen 80` riêng chỉ để redirect) — nếu domain của bạn bị đúng lỗi certbot thiếu block 443 đã ghi ở mục 6, xem lại đó trước khi áp dụng đoạn cấu hình trên.

```bash
nginx -t
systemctl reload nginx
```

### 15.4. Kiểm tra thực tế: giả lập 1 instance chết

```bash
# Terminal 1: theo dõi cả 2 instance
journalctl -u mqsocial-api@5000 -u mqsocial-api@5001 -f
```

```bash
# Terminal 2: gọi liên tục trong lúc test
watch -n 1 'curl -s -o /dev/null -w "%{http_code}\n" https://manager.mqsocial.vn/api/services/app/Session/GetCurrentLoginInformations'
```

```bash
# Terminal 3: giả lập crash 1 instance
systemctl stop mqsocial-api@5000
```

Kỳ vọng: `curl` ở Terminal 2 vẫn ra `200` liên tục (Nginx tự route hết sang `5001`), không có request nào bị `502`/timeout. Bật lại: `systemctl start mqsocial-api@5000`.

### 15.5. Redeploy code khi đã chạy nhiều instance

Không còn `mqsocial-api.service` nữa — thay các bước ở mục 8.4/8.2 bằng rolling restart từng instance một (không stop cả 2 cùng lúc):

```bash
# scp đè file mới vào /var/www/mqsocial/api/ như cũ, rồi:
systemctl stop mqsocial-api@5000
systemctl start mqsocial-api@5000
# đợi vài giây, xác nhận instance 5000 đã lên (systemctl status), rồi mới làm instance còn lại
systemctl stop mqsocial-api@5001
systemctl start mqsocial-api@5001
```

> Đây là lợi ích kép của việc load balance: **deploy code mới cũng không downtime** — luôn có ít nhất 1 instance sống phục vụ request trong lúc cập nhật instance kia.

Checklist mục 14 cũng đổi tương ứng: thay `systemctl status mqsocial-api` bằng `systemctl status mqsocial-api@5000 mqsocial-api@5001`.

> **Nếu có CI/CD (GitHub Actions `.github/workflows/deploy.yml` hoặc tương đương):** nhớ sửa luôn step restart backend — nó rất có thể đang gọi `systemctl restart mqsocial-api` (tên service đơn, đã bị xóa ở mục 15.2). Không sửa thì lần deploy tự động tiếp theo sẽ fail đúng ngay bước restart (`Unit mqsocial-api.service not found`), code mới đã upload lên server nhưng không bao giờ được áp dụng vì service không restart được. Đổi step đó thành rolling-restart lần lượt `mqsocial-api@5000` rồi `mqsocial-api@5001` (không đồng thời, giữ đúng nguyên tắc không-downtime ở trên) — xem `.github/workflows/deploy.yml` trong repo này để tham khảo bản đã sửa.

---

## Việc chưa làm / cần cân nhắc thêm sau này

- [ ] Backup off-site (đồng bộ file backup ra ngoài server, VD: rclone → Google Drive/S3) để tránh mất trắng nếu VPS gặp sự cố
- [ ] Nội dung thật cho landing page `mqsocial.vn` (hiện đang demo)
- [ ] Nâng cấp .NET 9 (STS, hết hỗ trợ 5/2026) lên .NET 10 LTS khi có thời gian
- [ ] Giới hạn/ẩn endpoint `/swagger` ở production nếu có bật
- [ ] HA thật sự (chống mất cả VPS): server thứ 2 độc lập + load balancer riêng + Postgres replication + DataProtection keys/SignalR dùng chung qua storage ngoài server — mục 15 mới chỉ chống được 1 process crash trên cùng 1 server



nano /etc/systemd/system/mqsocial-scheduler.service

Sửa dòng ExecStart thành (thêm đường dẫn dotnet vào trước):

ini
ExecStart=/usr/bin/dotnet /var/www/mqsocial/scheduler/schedule.dll

Lưu ý hai chỗ:

Phải là đường dẫn tuyệt đối tới dotnet. Bạn chạy which dotnet để lấy đúng — nếu ra /usr/bin/dotnet thì dùng như trên; nếu ra chỗ khác (ví dụ /usr/share/dotnet/dotnet) thì thay cho khớp.
Tên file là schedule.dll chứ không phải MqScheduler.dll như tôi đoán lúc trước — dùng đúng tên bạn có.

Rồi reload và restart:

bash
systemctl daemon-reload
systemctl restart mqsocial-scheduler
systemctl status mqsocial-scheduler