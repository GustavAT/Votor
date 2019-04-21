FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY *.sln .
COPY Votor/*.csproj ./Votor/
RUN dotnet restore

# copy everything else and build app
COPY Votor/. ./Votor/
WORKDIR /app/Votor
RUN dotnet publish -c Release -o out


FROM mcr.microsoft.com/dotnet/core/aspnet:2.2 AS runtime
WORKDIR /app
COPY --from=build /app/Votor/out ./
ENTRYPOINT ["dotnet", "Votor.dll"]