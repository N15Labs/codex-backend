# ===== Build stage =====
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything and restore/publish against the project file
COPY . .
RUN dotnet restore CodexBackend.csproj
RUN dotnet publish CodexBackend.csproj -c Release -o /app/out

# ===== Runtime stage =====
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Railway injects PORT; bind Kestrel to it
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT}

# (Optional on Railway; leaving 8080 is fine)
EXPOSE 8080

CMD ["dotnet", "CodexBackend.dll"]
