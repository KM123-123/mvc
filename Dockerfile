# Etapa 1: Compilar la aplicación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar proyecto y restaurar
COPY *.csproj .
RUN dotnet restore

# Copiar todo y publicar
COPY . .
RUN dotnet publish "mvc.csproj" -c Release -o /app/publish

# Etapa 2: Imagen runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Instalar soporte ICU para culturas (evita CultureNotFoundException)
RUN apt-get update \
 && apt-get install -y --no-install-recommends libicu-dev ca-certificates \
 && rm -rf /var/lib/apt/lists/*

# Permitir uso de culturas (no modo invariant)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Por conveniencia, establecer entorno que hará que la app cargue appsettings.Docker.json si existe
ENV ASPNETCORE_ENVIRONMENT=Docker

# 🔹 NUEVO: Forzar a la app a escuchar en el puerto 80
ENV ASPNETCORE_URLS=http://+:80

# Copiar los artefactos publicados
COPY --from=build /app/publish .

# Exponer puerto
EXPOSE 80

# Ejecutar la app
ENTRYPOINT ["dotnet", "mvc.dll"]