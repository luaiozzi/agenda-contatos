# Imagem da API. O contexto de build é a raiz do repositório (veja docker-compose.yml).

# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restaura primeiro só com o .csproj para aproveitar o cache de camadas do Docker.
COPY src/Agenda.Api/Agenda.Api.csproj src/Agenda.Api/
RUN dotnet restore src/Agenda.Api/Agenda.Api.csproj

COPY src/Agenda.Api/ src/Agenda.Api/
RUN dotnet publish src/Agenda.Api/Agenda.Api.csproj -c Release -o /app/publish --no-restore

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

# O banco SQLite fica em /data, que é um volume (os dados sobrevivem a recriações do container).
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__Default="Data Source=/data/agenda.db"

RUN mkdir -p /data && chown -R $APP_UID:$APP_UID /data

# Executa sem privilégios de root (usuário "app" das imagens oficiais do .NET 8).
USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "Agenda.Api.dll"]
