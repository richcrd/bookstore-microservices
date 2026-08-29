FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY BookStore.slnx .
COPY src/ ./src/
RUN dotnet restore src/Services/OrderSaga/OrderSaga.Worker/OrderSaga.Worker.csproj --verbosity quiet
RUN dotnet publish src/Services/OrderSaga/OrderSaga.Worker/OrderSaga.Worker.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrderSaga.Worker.dll"]