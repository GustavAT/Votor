FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build-env
WORKDIR /app
EXPOSE 44358 
EXPOSE 64913

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out

# Set secrets
COPY secrets.json ./
RUN cat ./secrets.json | dotnet user-secrets set

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/sdk:2.2
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "Votor.dll"]
