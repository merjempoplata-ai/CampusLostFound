FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .

# Explicitly restore + publish the exact project
RUN dotnet restore ./CampusLostAndFound.csproj
RUN dotnet publish ./CampusLostAndFound.csproj -c Release -o /app/publish

# Sanity check (will appear in Render build logs)
RUN ls -la /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:${PORT}
ENTRYPOINT ["dotnet", "CampusLostAndFound.dll"]
