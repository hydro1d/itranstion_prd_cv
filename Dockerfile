# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY ["src/CvPlatform/CvPlatform.csproj", "src/CvPlatform/"]
RUN dotnet restore "src/CvPlatform/CvPlatform.csproj"

# Copy full source code and publish release build
COPY . .
WORKDIR "/src/src/CvPlatform"
RUN dotnet publish "CvPlatform.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Minimal Production Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "CvPlatform.dll"]
