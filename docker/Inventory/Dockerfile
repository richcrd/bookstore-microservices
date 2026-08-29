FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY BookStore.slnx .
COPY src/ ./src/
RUN dotnet restore src/Services/Inventory/Inventory.API/Inventory.API.csproj --verbosity quiet
RUN dotnet publish src/Services/Inventory/Inventory.API/Inventory.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5208
EXPOSE 5208
ENTRYPOINT ["dotnet", "Inventory.API.dll"]