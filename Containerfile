FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY auth-worker.csproj ./
RUN dotnet restore ./auth-worker.csproj

COPY . ./

RUN dotnet publish ./auth-worker.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "auth-worker.dll"]
