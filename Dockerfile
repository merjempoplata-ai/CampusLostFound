# build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# copy everything
COPY . .

# restore + publish
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

# Render provides PORT
ENV ASPNETCORE_URLS=http://+:${PORT}

# run the app
ENTRYPOINT ["dotnet", "CampusLostAndFound.dll"]
