FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["FileAnalisysService.csproj", "./"]
RUN dotnet restore "./FileAnalisysService.csproj"


COPY . .
RUN dotnet publish "./FileAnalisysService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app

COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "FileAnalisysService.dll"]
