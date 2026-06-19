FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY ["src/MqSocial.Web.Host/MqSocial.Web.Host.csproj", "src/MqSocial.Web.Host/"]
COPY ["src/MqSocial.Web.Core/MqSocial.Web.Core.csproj", "src/MqSocial.Web.Core/"]
COPY ["src/MqSocial.Application/MqSocial.Application.csproj", "src/MqSocial.Application/"]
COPY ["src/MqSocial.Core/MqSocial.Core.csproj", "src/MqSocial.Core/"]
COPY ["src/MqSocial.EntityFrameworkCore/MqSocial.EntityFrameworkCore.csproj", "src/MqSocial.EntityFrameworkCore/"]
WORKDIR "/src/src/MqSocial.Web.Host"
RUN dotnet restore 

WORKDIR /src
COPY ["src/MqSocial.Web.Host", "src/MqSocial.Web.Host"]
COPY ["src/MqSocial.Web.Core", "src/MqSocial.Web.Core"]
COPY ["src/MqSocial.Application", "src/MqSocial.Application"]
COPY ["src/MqSocial.Core", "src/MqSocial.Core"]
COPY ["src/MqSocial.EntityFrameworkCore", "src/MqSocial.EntityFrameworkCore"]
WORKDIR "/src/src/MqSocial.Web.Host"
RUN dotnet publish -c Release -o /publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
EXPOSE 80
WORKDIR /app
COPY --from=build /publish .
ENTRYPOINT ["dotnet", "MqSocial.Web.Host.dll"]
