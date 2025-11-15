# ===== Etapa 1: Build =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

# Configurar zona horaria
ENV TZ=America/Guatemala
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

# Copiar solución y proyecto
COPY *.sln ./
COPY *.csproj ./

# Restaurar dependencias
RUN dotnet restore

# Copiar todo el código fuente
COPY . ./

# Compilar y publicar en Release
RUN dotnet publish -c Release -o /out

# ===== Etapa 2: Runtime =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# --- INICIO DE LA MODIFICACIÓN ---
# Instalar soporte de locales (Guatemala), ICU y LIBGDI+ para Excel
RUN apt-get update && apt-get install -y \
        locales \
        icu-devtools \
        libgdiplus \
    && locale-gen es_GT.UTF-8 \
    && rm -rf /var/lib/apt/lists/*
# --- FIN DE LA MODIFICACIÓN ---

# Copiar la salida de compilación desde la etapa build
COPY --from=build /out ./

# Configurar variables de entorno
ENV ASPNETCORE_URLS=http://+:80
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
# --- INICIO DE LA MODIFICACIÓN ---
# Configurar locale para Guatemala
ENV LANG=es_GT.UTF-8
ENV LANGUAGE=es_GT:es
ENV LC_ALL=es_GT.UTF-8
# --- FIN DE LA MODIFICACIÓN ---

# Exponer puerto
EXPOSE 80

# Configurar entorno temporalmente como Development para ver errores
ENV ASPNETCORE_ENVIRONMENT=Development

# Comando de inicio
ENTRYPOINT ["dotnet", "mvc.dll"]