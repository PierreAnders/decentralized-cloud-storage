#!/bin/bash

# Démarrer le démon IPFS
ipfs daemon &

# Attendre quelques secondes pour s'assurer que le démon IPFS est bien démarré
sleep 5

# Naviguer vers le répertoire TheMerkleTrees/TheMerkleTrees.Api et exécuter dotnet run
cd TheMerkleTrees/TheMerkleTrees.Api || { echo "Répertoire TheMerkleTrees/TheMerkleTrees.Api introuvable"; exit 1; }
dotnet run &

# Revenir à la racine
cd ../../

# Naviguer vers le répertoire TheMerkleTreesClient et exécuter npm run dev
cd TheMerkleTreesClient || { echo "Répertoire TheMerkleTreesClient introuvable"; exit 1; }
npm run dev


# chmod +x run_merkle.sh

# ./run_merkle.sh