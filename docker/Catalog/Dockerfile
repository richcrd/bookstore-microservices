FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY BookStore.slnx .
COPY src/ ./src/
RUN dotnet restore src/Services/Catalog/Catalog.API/Catalog.API.csproj --verbosity quiet
RUN dotnet publish src/Services/Catalog/Catalog.API/Catalog.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5038
EXPOSE 5038
ENTRYPOINT ["dotnet", "Catalog.API.dll"]