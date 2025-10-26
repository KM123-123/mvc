# Usar una imagen oficial de .NET para compilar y publicar
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

ENV TZ=America/Guatemala
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

# Copiar archivos del proyecto
COPY . ./

# Restaurar dependencias
RUN dotnet restore

# Compilar y publicar en modo release
RUN dotnet publish -c Release -o /out

# Imagen ligera para ejecución
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copiar la salida de la compilación
COPY --from=build /out ./

# Configurar variables de entorno
ENV ASPNETCORE_URLS=http://+:80
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# Exponer el puerto 8080
EXPOSE 80

#COPY junio.pfx /junio.pfx

# Comando de inicio
ENTRYPOINT ["dotnet", "mvc.dll"]