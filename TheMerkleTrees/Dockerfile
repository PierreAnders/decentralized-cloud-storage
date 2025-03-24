# Étape 1 : Build (construction de l'application)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-env
WORKDIR /app

# Copier le fichier .sln
COPY TheMerkleTrees.sln ./

# Copier les fichiers .csproj et restaurer les dépendances
COPY TheMerkleTrees.Api/TheMerkleTrees.Api.csproj ./TheMerkleTrees.Api/
COPY TheMerkleTrees.Domain/TheMerkleTrees.Domain.csproj ./TheMerkleTrees.Domain/
COPY TheMerkleTrees.Infrastructure/TheMerkleTrees.Infrastructure.csproj ./TheMerkleTrees.Infrastructure/
RUN dotnet restore

# Copier le reste des fichiers du projet
COPY . ./

# Publier l'application
RUN dotnet publish TheMerkleTrees.Api/TheMerkleTrees.Api.csproj -c Release -o TheMerkleTrees.Api/out

# Étape 2 : Runtime (exécution de l'application)
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Copier les fichiers publiés depuis l'étape précédente
COPY --from=build-env /app/TheMerkleTrees.Api/out ./

# Définir les variables d'environnement (si nécessaire)
ENV ASPNETCORE_URLS=http://+:80

# Copier le fichier appsettings.json
COPY TheMerkleTrees.Api/appsettings.json ./

# Commande pour démarrer l'application
ENTRYPOINT ["dotnet", "TheMerkleTrees.Api.dll"]

