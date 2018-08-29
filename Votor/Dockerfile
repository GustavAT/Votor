FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 51031
EXPOSE 44337

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY Votor/Votor.csproj Votor/
RUN dotnet restore Votor/Votor.csproj
COPY . .
WORKDIR /src/Votor
RUN dotnet build Votor.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Votor.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Votor.dll"]
