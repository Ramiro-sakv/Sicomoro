FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Sicomoro.sln ./
COPY Sicomoro.Domain/Sicomoro.Domain.csproj Sicomoro.Domain/
COPY Sicomoro.Application/Sicomoro.Application.csproj Sicomoro.Application/
COPY Sicomoro.Infrastructure/Sicomoro.Infrastructure.csproj Sicomoro.Infrastructure/
COPY Sicomoro.Api/Sicomoro.Api.csproj Sicomoro.Api/
RUN dotnet restore Sicomoro.Api/Sicomoro.Api.csproj
COPY . .
RUN dotnet publish Sicomoro.Api/Sicomoro.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
COPY Sicomoro.Frontend ./wwwroot
EXPOSE 8080
ENTRYPOINT ["dotnet", "Sicomoro.Api.dll"]
