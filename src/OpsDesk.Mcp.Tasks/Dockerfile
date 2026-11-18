FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/OpsDesk.Mcp.Tasks/ src/OpsDesk.Mcp.Tasks/
RUN dotnet publish src/OpsDesk.Mcp.Tasks/OpsDesk.Mcp.Tasks.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "OpsDesk.Mcp.Tasks.dll"]
