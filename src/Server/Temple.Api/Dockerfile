FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/Server/Temple.sln ./
COPY src/Server/Temple.Domain/Temple.Domain.csproj src/Server/Temple.Domain/
COPY src/Server/Temple.Application/Temple.Application.csproj src/Server/Temple.Application/
COPY src/Server/Temple.Infrastructure/Temple.Infrastructure.csproj src/Server/Temple.Infrastructure/
COPY src/Server/Temple.Api/Temple.Api.csproj src/Server/Temple.Api/
RUN dotnet restore src/Server/Temple.Api/Temple.Api.csproj
COPY . .
WORKDIR /src/src/Server/Temple.Api
RUN dotnet publish Temple.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet","Temple.Api.dll"]
