FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Reklaim.api/Reklaim.api.csproj", "Reklaim.api/"]
RUN dotnet restore "Reklaim.api/Reklaim.api.csproj"

COPY . .
WORKDIR /src/Reklaim.api
RUN dotnet publish "Reklaim.api.csproj" \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Reklaim.api.dll"]
