# Dockerfile para despliegue en Render
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar csproj y restaurar dependencias
COPY PlataformaIncidencias/PlataformaIncidencias.csproj PlataformaIncidencias/
RUN dotnet restore PlataformaIncidencias/PlataformaIncidencias.csproj

# Copiar todo el código fuente y publicar
COPY PlataformaIncidencias/ PlataformaIncidencias/
WORKDIR /src/PlataformaIncidencias
RUN dotnet publish -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Variables de entorno por defecto
ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PlataformaIncidencias.dll"]
