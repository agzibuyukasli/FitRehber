# ────────────────────────────────────────────
#  Stage 1 – base runtime image
# ────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8443

# ────────────────────────────────────────────
#  Stage 2 – build
#  Build context = repo root (bkz. docker-compose.yml)
# ────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Sadece .csproj dosyalarini kopyala → layer cache optimizasyonu
COPY src/DietitianClinic.Entity/DietitianClinic.Entity.csproj          src/DietitianClinic.Entity/
COPY src/DietitianClinic.DataAccess/DietitianClinic.DataAccess.csproj  src/DietitianClinic.DataAccess/
COPY src/DietitianClinic.Business/DietitianClinic.Business.csproj      src/DietitianClinic.Business/
COPY src/DietitianClinic.API/DietitianClinic.API.csproj                src/DietitianClinic.API/

RUN dotnet restore src/DietitianClinic.API/DietitianClinic.API.csproj

# Kaynak kodun tamamini kopyala ve derle
COPY src/ src/
WORKDIR /src/src/DietitianClinic.API
RUN dotnet build -c Release -o /app/build --no-restore

# ────────────────────────────────────────────
#  Stage 3 – publish
# ────────────────────────────────────────────
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish --no-restore

# ────────────────────────────────────────────
#  Stage 4 – final image
# ────────────────────────────────────────────
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Sağlıklı uygulama için non-root kullanıcı
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

ENTRYPOINT ["dotnet", "DietitianClinic.API.dll"]
