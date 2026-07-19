# Hướng dẫn Deploy MqSocial (ASP.NET Zero + Angular + PostgreSQL) trên Ubuntu

> Ghi chú: guide này đúc kết từ lần deploy thực tế lên server `103.162.21.123` / domain `mqsocial.vn`. Khi deploy server mới, thay các giá trị (IP, domain, mật khẩu) cho phù hợp.

**Kiến trúc tổng quan:**

```
mqsocial.vn / www.mqsocial.vn      → Landing page (static, Nginx serve)
manager.mqsocial.vn                → Angular (Nginx static)
                                       + proxy /api, /signalr, route gốc ABP → localhost:5000
                                                    ↓
                                       MqSocial.Web.Host (systemd, port 5000)
                                                    ↓
                                       PostgreSQL (mqsocial_db, localhost:5432)
                                                    ↑
                                       MqSocial.Scheduler (Hangfire, systemd, dùng chung DB)
```

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
scp -r publish\* root@<IP_SERVER>:/var/www/mqsocial/api/
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
scp -r publish\* root@<IP_SERVER>:/var/www/mqsocial/migrator/
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

```powershell
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
scp -r dist\browser\* root@<IP_SERVER>:/var/www/mqsocial/manager/
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

## 12. Deploy MqScheduler (Hangfire, console app)

### 12.1. Build (self-contained executable)

```powershell
cd aspnet-core\src\MqSocial.Scheduler
dotnet publish -c Release -o publish
```

### 12.2. Kiểm tra connection string trong `publish\appsettings.json`

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=mqsocial_db;Username=mqsocial_user;Password=MẬT_KHẨU"
  }
}
```

### 12.3. Upload

```bash
mkdir -p /var/www/mqsocial/scheduler
```

```powershell
scp -r publish\* root@<IP_SERVER>:/var/www/mqsocial/scheduler/
```

### 12.4. Test chạy tay trước

```bash
cd /var/www/mqsocial/scheduler
chmod +x ./<tên_binary>          # ví dụ: schedule
./<tên_binary>
```

Kỳ vọng: Hangfire khởi động, đăng ký recurring jobs (`CrawlTikTokJob`, `SyncKolJob`, `UpdateContractKolResultJob`...) không lỗi. `Ctrl+C` để dừng.

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
ExecStart=/var/www/mqsocial/scheduler/<tên_binary>
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

> Nếu publish dạng framework-dependent (không phải self-contained), `ExecStart` cần dạng `/usr/bin/dotnet /đường/dẫn/MqSocial.Scheduler.dll` thay vì gọi thẳng binary.

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
```

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

## Việc chưa làm / cần cân nhắc thêm sau này

- [ ] Backup off-site (đồng bộ file backup ra ngoài server, VD: rclone → Google Drive/S3) để tránh mất trắng nếu VPS gặp sự cố
- [ ] Nội dung thật cho landing page `mqsocial.vn` (hiện đang demo)
- [ ] Nâng cấp .NET 9 (STS, hết hỗ trợ 5/2026) lên .NET 10 LTS khi có thời gian
- [ ] Giới hạn/ẩn endpoint `/swagger` ở production nếu có bật
