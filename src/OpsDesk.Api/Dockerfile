FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/OpsDesk.Api/ src/OpsDesk.Api/
RUN dotnet publish src/OpsDesk.Api/OpsDesk.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# SQLite file lives in /data; mount an Azure Files volume there to persist, or leave ephemeral (reset on restart)
ENV ConnectionStrings__DefaultConnection="Data Source=/data/opsdesk.db"
RUN mkdir -p /data
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "OpsDesk.Api.dll"]
