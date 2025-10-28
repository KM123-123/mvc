# Etapa 1: Construir la aplicación
# Usamos la imagen oficial del SDK de .NET 8 (cambia el 8.0 si usas otra versión)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar los archivos de proyecto y restaurar dependencias
COPY *.csproj .
RUN dotnet restore

# Copiar todo el resto del código y construir la app
COPY . .
RUN dotnet publish "mvc.csproj" -c Release -o /app/publish

# Etapa 2: Crear la imagen final
# Usamos la imagen de runtime, que es más ligera
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Reemplaza "TuProyecto.dll" con el nombre real de la DLL de tu proyecto
ENTRYPOINT ["dotnet", "mvc.dll"]