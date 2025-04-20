<template>
  <div class="min-h-screen px-8 pt-8">
    <BurgerMenu />
    <div class="flex items-center justify-center pt-8">
      <h1 class="pr-3 tracking-wider text-light-gray">
        {{ folderName.toUpperCase() }}
      </h1>
      <IconFolder />
    </div>

    <!-- Fil d'Ariane -->
    <div class="flex items-center justify-center mt-4 text-sm text-light-gray">
      <span @click="navigateToDocuments" class="cursor-pointer hover:text-white">
        Documents
      </span>
      <template v-for="(folder, index) in breadcrumb" :key="folder.id">
        <span class="mx-1">/</span>
        <span @click="navigateToBreadcrumbFolder(folder.id)" class="cursor-pointer hover:text-white">
          {{ folder.name }}
        </span>
      </template>
      <span class="mx-1">/</span>
      <span class="text-white">{{ folderName }}</span>
    </div>

    <!-- Liste des sous-dossiers -->
    <div v-if="subfolders.length > 0" class="w-3/4 mx-auto mt-8 md:w-2/3 lg:w-1/2">
      <ul class="flex flex-col">
        <li v-for="folder in subfolders" :key="folder.id"
          class="flex flex-col justify-between mt-4 text-white md:flex-row">
          <div class="flex justify-between w-full">
            <div class="flex items-center space-x-2 cursor-pointer" @click="navigateToSubfolder(folder.id)">
              <IconFolder :color="'#828282'" iclass="opacity-50" />
              <span class="text-sm">{{ folder.name }}</span>
            </div>
            <div class="flex items-center space-x-2">
              <button @click="showRenameFolder(folder)">
                <IconEdit :color="'#553348'"
                  class="w-5 h-5 transition-transform transform md:w-6 md:h-6 hover:scale-110" />
              </button>
              <button @click="deleteFolder(folder.id)">
                <IconSubmenuDeleteFolder :color="'#553348'"
                  class="w-5 h-5 transition-transform transform md:w-6 md:h-6 hover:scale-110" />
              </button>
            </div>
          </div>
        </li>
      </ul>
    </div>

    <!-- Liste des fichiers -->
    <ul class="flex flex-col w-3/4 mx-auto mt-8 mb-8 md:w-2/3 lg:w-1/2">
      <li v-for="file in processedFileList" :key="file.id"
        class="flex flex-col justify-between mt-4 text-white md:flex-row">
        <div class="flex justify-between w-full">
          <div class="flex items-center space-x-2">
            <IconDocument :color="'#828282'" iclass="opacity-50" />
            <span class="text-sm">{{ file.displayName }}</span>
          </div>
          <div class="flex items-center space-x-2">
            <button @click="deleteFile(file.id)">
              <IconSubmenuDeleteFolder :color="'#553348'"
                class="w-5 h-5 transition-transform transform md:w-6 md:h-6 hover:scale-110" />
            </button>

            <button @click="downloadFile(file.name)">
              <IconDownload class="w-5 h-5 transition-transform transform md:w-6 md:h-6 hover:scale-110" />
            </button>
            <button @click="openFile(file.name)">
              <IconOpen class="w-5 h-5 transition-transform transform md:w-6 md:h-6 hover:scale-110" />
            </button>
          </div>
        </div>
        <!-- Afficher les métadonnées déchiffrées si disponibles -->
        <div v-if="file.metadata && file.metadata.description" class="mt-1 text-sm text-light-gray">
          {{ file.metadata.description }}
        </div>
      </li>
    </ul>

    <!-- Actions -->
    <div class="pt-4 flex justify-center space-x-8">

      <!-- Créer un sous-dossier -->
      <div class="flex flex-col justify-center">
        <div
          class="flex flex-col justify-center cursor-pointer text-light-gray transition-transform transform hover:scale-110"
          @click="showCreateFolderModal">
          <IconNewFolder :color="'#3A3A3A'" class="pb-2 mx-auto w-24" />
          <div class="flex flex-col items-center">
            <h2 class="text-light-gray text-sm">Créer un dossier</h2>
          </div>
        </div>
      </div>

      <!-- Ajouter un fichier -->
      <div class="flex flex-col justify-center">
        <div id="fileNameLabel"
          class="flex flex-col justify-center cursor-pointer text-light-gray transition-transform transform hover:scale-110">
          <label for="fileInput" class="text-light-gray">
            <IconUpload class="pb-2 mx-auto w-14" />
            <input type="file" id="fileInput" ref="fileInput"
              accept=".pdf,.doc,.docx,.xls,.xlsx,.txt,.md,.html,.mp3,.mp4,.png,.jpg,.jpeg,.gif,mpeg,.webm,.ogg,.wav,.flac,.mp3,.mp4,.avi,.mov,.wmv,.mkv,.flv,.webm,.ogg,.wav,.flac,.mp3,.mp4,.avi,.mov"
              class="pb-2 border rounded-md bg-neutral-300 text-neutral-800 focus:outline-none focus:border-amber-800"
              @change="showFileMetadataModal" style="display: none" />
          </label>
          <div class="flex flex-col items-center">
            <h2 class="text-light-gray text-sm">Ajouter un fichier</h2>
            <!-- <div class="mt-1">
              <label for="isPublic" class="text-light-gray text-sm mr-2">Public</label>
              <input type="checkbox" class="opacity-90" name="isPublic" id="isPublic" v-model="isPublic" />
            </div> -->
          </div>
        </div>
      </div>
    </div>

    <!-- Modal pour créer/renommer un dossier -->
    <div v-if="showFolderModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
      <div class="p-6 bg-dark-gray rounded-lg shadow-lg">
        <h2 class="mb-4 text-xl text-white">{{ editingFolder ? 'Renommer le dossier' : 'Créer un sous-dossier' }}</h2>
        <input v-model="newFolderName" class="w-full p-2 mb-4 text-white bg-black border border-gray-700 rounded"
          placeholder="Nom du dossier" @keyup.enter="editingFolder ? updateFolder() : createSubfolder()" />
        <div class="flex justify-between">
          <button @click="closeFolderModal" class="px-4 py-2 text-white bg-gray-700 rounded hover:bg-gray-600">
            Annuler
          </button>
          <button @click="editingFolder ? updateFolder() : createSubfolder()"
            class="px-4 py-2 text-white bg-blue-600 rounded hover:bg-blue-500">
            {{ editingFolder ? 'Renommer' : 'Créer' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Modal pour les métadonnées du fichier -->
    <div v-if="showMetadataModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
      <div class="p-6 bg-dark-gray rounded-lg shadow-lg w-full max-w-md">
        <h2 class="mb-4 text-xl text-white">Métadonnées du fichier</h2>

        <div class="mb-4">
          <label class="block text-sm text-light-gray mb-1">Nom d'affichage</label>
          <input v-model="fileMetadata.displayName"
            class="w-full p-2 text-white bg-black border border-gray-700 rounded" placeholder="Nom d'affichage" />
        </div>

        <div class="mb-4">
          <label class="block text-sm text-light-gray mb-1">Description</label>
          <textarea v-model="fileMetadata.description"
            class="w-full p-2 text-white bg-black border border-gray-700 rounded h-24"
            placeholder="Description du fichier"></textarea>
        </div>

        <div class="mb-4">
          <label class="block text-sm text-light-gray mb-1">Tags (séparés par des virgules)</label>
          <input v-model="fileMetadata.tags" class="w-full p-2 text-white bg-black border border-gray-700 rounded"
            placeholder="tag1, tag2, tag3" />
        </div>

        <div class="flex justify-between">
          <button @click="closeMetadataModal" class="px-4 py-2 text-white bg-gray-700 rounded hover:bg-gray-600">
            Annuler
          </button>
          <button @click="uploadFileWithMetadata" class="px-4 py-2 text-white bg-blue-600 rounded hover:bg-blue-500">
            Téléverser
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import axios from "axios";
import { useTextContentStore } from "../../textContentStore.js";
import pbkdf2CryptoService from "../../pbkdf2CryptoService.js";
const BASE_URL = import.meta.env.VITE_BASE_URL;

export default {
  data() {
    return {
      fileList: [],
      processedFileList: [], // Liste des fichiers avec métadonnées déchiffrées
      folderName: "",
      currentFolderId: null,
      subfolders: [],
      breadcrumb: [],
      isPublic: false,
      showFolderModal: false,
      newFolderName: "",
      editingFolder: null,

      // Nouvelles propriétés pour les métadonnées
      showMetadataModal: false,
      selectedFile: null,
      fileMetadata: {
        displayName: "",
        description: "",
        tags: ""
      }
    };
  },
  mounted() {
    // Récupérer le nom du dossier depuis l'URL
    this.folderName = this.$route.params.folderName;

    // Récupérer l'ID du dossier depuis les paramètres de requête
    this.currentFolderId = this.$route.query.folderId;

    // Si l'ID du dossier est fourni, l'utiliser directement
    if (this.currentFolderId) {
      this.loadFolder(this.currentFolderId);
    }
    // Sinon, essayer de trouver le dossier par son nom (pour la compatibilité)
    else {
      this.findFolderByName(this.folderName);
    }
  },
  methods: {
    // Méthode pour trouver un dossier par son nom (pour la compatibilité)
    async findFolderByName(folderName) {
      const jwtToken = this.getJwtToken();

      try {
        // Récupérer tous les dossiers racine
        const response = await axios.get(`${BASE_URL}/api/Folders/root`, {
          headers: {
            Authorization: `Bearer ${jwtToken}`,
            "Content-Type": "application/json",
          }
        });

        if (response.status === 200) {
          // Chercher le dossier par son nom
          const folder = response.data.find(f => f.name === folderName);
          if (folder) {
            this.currentFolderId = folder.id;
            this.loadFolder(folder.id);
          } else {
            console.error("Dossier non trouvé");
          }
        }
      } catch (error) {
        console.error("Erreur lors de la recherche du dossier:", error);
      }
    },

    // Méthode pour charger un dossier et ses sous-dossiers
    async loadFolder(folderId) {
      const jwtToken = this.getJwtToken();

      try {
        // Charger les détails du dossier
        const folderResponse = await axios.get(`${BASE_URL}/api/Folders/${folderId}`, {
          headers: {
            Authorization: `Bearer ${jwtToken}`,
            "Content-Type": "application/json",
          }
        });

        if (folderResponse.status === 200) {
          const folder = folderResponse.data;
          this.folderName = folder.name;

          // Mettre à jour l'URL sans recharger la page
          this.$router.replace({
            params: { folderName: this.folderName },
            query: { folderId: folderId }
          });

          // Charger le fil d'Ariane
          this.loadBreadcrumb(folder);

          // Charger les sous-dossiers
          this.loadSubfolders(folderId);

          // Charger les fichiers du dossier
          this.loadFileList(folderId);
        }
      } catch (error) {
        console.error("Erreur lors du chargement du dossier:", error);
      }
    },

    // Méthode pour charger les sous-dossiers
    async loadSubfolders(folderId) {
      const jwtToken = this.getJwtToken();

      try {
        const response = await axios.get(`${BASE_URL}/api/Folders/${folderId}/subfolders`, {
          headers: {
            Authorization: `Bearer ${jwtToken}`,
            "Content-Type": "application/json",
          }
        });

        if (response.status === 200) {
          this.subfolders = response.data;
        }
      } catch (error) {
        console.error("Erreur lors du chargement des sous-dossiers:", error);
      }
    },

    // Méthode pour construire le fil d'Ariane
    async loadBreadcrumb(folder) {
      this.breadcrumb = [];
      await this.buildBreadcrumb(folder);
    },

    // Méthode récursive pour construire le fil d'Ariane
    async buildBreadcrumb(folder) {
      if (!folder || !folder.parentId) {
        return;
      }

      const jwtToken = this.getJwtToken();

      try {
        const response = await axios.get(`${BASE_URL}/api/Folders/${folder.parentId}`, {
          headers: {
            Authorization: `Bearer ${jwtToken}`,
            "Content-Type": "application/json",
          }
        });

        if (response.status === 200) {
          const parentFolder = response.data;
          this.breadcrumb.unshift(parentFolder);
          await this.buildBreadcrumb(parentFolder);
        }
      } catch (error) {
        console.error("Erreur lors du chargement du chemin de navigation:", error);
      }
    },

    // Méthode pour naviguer vers un sous-dossier
    navigateToSubfolder(subfolderId) {
      const subfolder = this.subfolders.find(f => f.id === subfolderId);
      if (subfolder) {
        this.$router.push(`/folders/${subfolder.name}?folderId=${subfolderId}`);
      }
    },

    // Méthode pour revenir à la page documents
    navigateToDocuments() {
      this.$router.push('/documents');
    },

    // Méthode pour naviguer vers un dossier parent dans le fil d'Ariane
    navigateToBreadcrumbFolder(folderId) {
      const folder = this.breadcrumb.find(f => f.id === folderId);
      if (folder) {
        this.$router.push(`/folders/${folder.name}?folderId=${folderId}`);
      }
    },

    // Méthode pour afficher le modal de création de sous-dossier
    showCreateFolderModal() {
      this.editingFolder = null;
      this.newFolderName = "";
      this.showFolderModal = true;
    },

    // Méthode pour afficher le modal de renommage de dossier
    showRenameFolder(folder) {
      this.editingFolder = folder;
      this.newFolderName = folder.name;
      this.showFolderModal = true;
    },

    // Méthode pour fermer le modal
    closeFolderModal() {
      this.showFolderModal = false;
      this.newFolderName = "";
      this.editingFolder = null;
    },

    // Méthode pour créer un sous-dossier
    async createSubfolder() {
      if (!this.newFolderName.trim()) {
        alert("Veuillez entrer un nom de dossier");
        return;
      }

      const jwtToken = this.getJwtToken();

      const newFolder = {
        name: this.newFolderName.trim(),
        parentId: this.currentFolderId,
        isPublic: false
      };

      try {
        const response = await axios.post(`${BASE_URL}/api/Folders`, newFolder, {
          headers: {
            Authorization: `Bearer ${jwtToken}`,
            "Content-Type": "application/json",
          },
        });

        if (response.status === 201) {
          this.loadSubfolders(this.currentFolderId);
          this.closeFolderModal();
        }
      } catch (error) {
        console.error("Erreur lors de la création du sous-dossier:", error);
        alert("Erreur lors de la création du sous-dossier");
      }
    },

    // Méthode pour renommer un dossier
    async updateFolder() {
      if (!this.newFolderName.trim()) {
        alert("Veuillez entrer un nom de dossier");
        return;
      }

      const jwtToken = this.getJwtToken();

      const updateData = {
        name: this.newFolderName.trim(),
        parentId: this.editingFolder.parentId,
        isPublic: this.editingFolder.isPublic
      };

      try {
        const response = await axios.put(`${BASE_URL}/api/Folders/${this.editingFolder.id}`, updateData, {
          headers: {
            Authorization: `Bearer ${jwtToken}`,
            "Content-Type": "application/json",
          },
        });

        if (response.status === 200) {
          // Si c'est le dossier courant qui est renommé
          if (this.editingFolder.id === this.currentFolderId) {
            this.folderName = this.newFolderName.trim();
            // Mettre à jour l'URL
            this.$router.replace({
              params: { folderName: this.folderName },
              query: { folderId: this.currentFolderId }
            });
          }

          this.loadSubfolders(this.currentFolderId);
          this.closeFolderModal();
        }
      } catch (error) {
        console.error("Erreur lors de la mise à jour du dossier:", error);
        alert("Erreur lors de la mise à jour du dossier");
      }
    },

    // Méthode pour supprimer un dossier
    async deleteFolder(folderId) {
      if (!confirm("Êtes-vous sûr de vouloir supprimer ce dossier et tout son contenu ?")) {
        return;
      }

      const jwtToken = this.getJwtToken();

      try {
        const response = await axios.delete(`${BASE_URL}/api/Folders/${folderId}`, {
          headers: {
            Authorization: `Bearer ${jwtToken}`,
            "Content-Type": "application/json",
          },
        });

        if (response.status === 204) {
          this.loadSubfolders(this.currentFolderId);
        }
      } catch (error) {
        console.error("Erreur lors de la suppression du dossier:", error);
        alert("Erreur lors de la suppression du dossier");
      }
    },

    // Méthode pour charger les fichiers d'un dossier
    async loadFileList(folderId) {
      if (process.client) {
        const jwtToken = this.getJwtToken();

        try {
          const response = await axios.get(`${BASE_URL}/api/Files/folder/${folderId}`, {
            headers: {
              Authorization: `Bearer ${jwtToken}`,
              "Content-Type": "application/json",
            },
          });

          this.fileList = response.data;

          // Traiter les métadonnées des fichiers
          await this.processFileMetadata();
        } catch (error) {
          console.error("Erreur lors du chargement des fichiers:", error);
        }
      }
    },

    // Méthode pour traiter les métadonnées des fichiers
    async processFileMetadata() {
      this.processedFileList = [...this.fileList];

      // Si l'utilisateur n'a pas de mot de passe, on ne peut pas déchiffrer les métadonnées
      if (!pbkdf2CryptoService.hasUserPassword()) {
        // Utiliser le nom du fichier comme nom d'affichage
        this.processedFileList.forEach(file => {
          file.displayName = file.name;
        });
        return;
      }

      // Déchiffrer les métadonnées pour chaque fichier non public qui en possède
      for (let i = 0; i < this.processedFileList.length; i++) {
        const file = this.processedFileList[i];

        // Par défaut, utiliser le nom du fichier comme nom d'affichage
        file.displayName = file.name;

        // Si le fichier a des métadonnées chiffrées et n'est pas public, essayer de les déchiffrer
        if (!file.isPublic && file.encryptedMetadata && file.metadataSalt && file.metadataIV) {
          try {
            const metadata = await pbkdf2CryptoService.decryptMetadata(
              file.encryptedMetadata,
              file.metadataSalt,
              file.metadataIV
            );

            file.metadata = metadata;

            // Utiliser le nom d'affichage des métadonnées s'il existe
            if (metadata.displayName) {
              file.displayName = metadata.displayName;
            }
          } catch (error) {
            console.error(`Erreur lors du déchiffrement des métadonnées pour ${file.name}:`, error);
          }
        }
      }
    },

    // Méthode pour supprimer un fichier
    deleteFile(fileId) {
      console.log("Suppression du fichier avec l'ID:", fileId);
      if (!confirm("Êtes-vous sûr de vouloir supprimer ce fichier ?")) {
        return;
      }

      const jwtToken = this.getJwtToken();

      axios
        .delete(`${BASE_URL}/api/Files/${fileId}`, {
          headers: {
            Authorization: `Bearer ${jwtToken}`,
            "Content-Type": "application/json",
          },
        })
        .then(() => {
          this.loadFileList(this.currentFolderId);
        })
        .catch((error) => {
          console.error(error);
          alert("Erreur lors de la suppression du fichier");
        });
    },

    // Méthode pour télécharger un fichier
    async downloadFile(fileName) {
      try {
        const jwtToken = this.getJwtToken();

        if (!this.isFilePublic(fileName) && !pbkdf2CryptoService.hasUserPassword()) {
          const password = await this.promptForPassword();
          if (!password) {
            alert("Mot de passe requis pour déchiffrer le fichier.");
            return;
          }
          pbkdf2CryptoService.setUserPassword(password);
        }

        const axiosConfig = {
          headers: {
            Authorization: `Bearer ${jwtToken}`,
          },
          responseType: "arraybuffer",
        };

        const response = await axios.get(
          `${BASE_URL}/api/Files/file/${fileName}`,
          axiosConfig
        );

        if (this.isFilePublic(fileName)) {
          const blob = new Blob([response.data]);
          this.saveBlob(blob, fileName);
          return;
        }

        // Récupérer les paramètres cryptographiques (sel et IV)
        const cryptoParamsResponse = await axios.get(
          `${BASE_URL}/api/Files/crypto-params/${fileName}`,
          {
            headers: {
              Authorization: `Bearer ${jwtToken}`,
            },
          }
        );

        const salt = cryptoParamsResponse.data.salt;
        const iv = cryptoParamsResponse.data.iv;

        if (!salt) {
          console.error("Sel de déchiffrement non trouvé pour ce fichier");
          alert("Impossible de déchiffrer ce fichier. Le sel n'est pas disponible.");
          return;
        }

        const fileContent = await pbkdf2CryptoService.decryptData(
          response.data,
          salt,
          iv
        );

        const blob = new Blob([fileContent]);
        this.saveBlob(blob, fileName);
      } catch (error) {
        console.error("Erreur lors du téléchargement:", error);

        if (error.message && error.message.includes("déchiffrement")) {
          alert("Erreur de déchiffrement. Le mot de passe est peut-être incorrect.");
          pbkdf2CryptoService.clearUserPassword();
        } else {
          alert("Erreur lors du téléchargement du fichier.");
        }
      }
    },

    saveBlob(blob, fileName) {
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.setAttribute("download", fileName);
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    },

    async openFile(fileName) {
      try {
        const jwtToken = this.getJwtToken();
        const fileType = this.getFileType(fileName);

        if (!this.isFilePublic(fileName) && !pbkdf2CryptoService.hasUserPassword()) {
          const password = await this.promptForPassword();
          if (!password) {
            alert("Mot de passe requis pour déchiffrer le fichier.");
            return;
          }
          pbkdf2CryptoService.setUserPassword(password);
        }

        const axiosConfig = {
          headers: { Authorization: `Bearer ${jwtToken}` },
          responseType: "arraybuffer",
        };

        const response = await axios.get(
          `${BASE_URL}/api/Files/file/${fileName}`,
          axiosConfig
        );

        let fileContent;

        if (this.isFilePublic(fileName)) {
          fileContent = response.data;
        } else {
          // Récupérer les paramètres cryptographiques (sel et IV)
          const cryptoParamsResponse = await axios.get(
            `${BASE_URL}/api/Files/crypto-params/${fileName}`,
            {
              headers: {
                Authorization: `Bearer ${jwtToken}`,
              },
            }
          );

          const salt = cryptoParamsResponse.data.salt;
          const iv = cryptoParamsResponse.data.iv;

          if (!salt) {
            console.error("Sel de déchiffrement non trouvé pour ce fichier");
            alert("Impossible de déchiffrer ce fichier. Le sel n'est pas disponible.");
            return;
          }

          fileContent = await pbkdf2CryptoService.decryptData(
            response.data,
            salt,
            iv
          );
        }

        if (
          fileType === "pdf" ||
          fileType === "html" ||
          fileType === "txt" ||
          fileType === "md" ||
          fileType === "csv" ||
          fileType === "xml" ||
          fileType === "json" ||
          fileType === "svg" ||
          fileType === "webp" ||
          fileType === "ico" ||
          fileType === "js" ||
          fileType === "css" ||
          ["png", "jpg", "jpeg", "gif", "mp3", "mp4", "webm", "wav", "ogg", "mov"].includes(fileType)
        ) {
          let type_blob;

          if (fileType === "pdf") {
            type_blob = "application/pdf";
          }
          else if (["png", "jpg", "jpeg", "gif", "svg", "webp", "ico"].includes(fileType)) {
            type_blob = fileType === "svg" ? "image/svg+xml" :
              fileType === "ico" ? "image/x-icon" :
                `image/${fileType}`;
          }
          else if (fileType === "txt" || fileType === "md") {
            const decoder = new TextDecoder("utf-8");
            const decodedText = decoder.decode(fileContent);
            this.displayTextFile(decodedText, true);
            return;
          }
          if (fileType === "html") {
            const fileNameWithoutExtension = fileName.replace(/\.[^/.]+$/, "");
            const decoder = new TextDecoder("utf-8");
            const decodedText = decoder.decode(fileContent);

            // Utiliser le textContentStore pour stocker le contenu
            useTextContentStore().setTextContent(decodedText);
            useTextContentStore().setFileNameWithoutExtension(fileNameWithoutExtension);

            this.$router.push("/note");
            return;
          }
          else if (fileType === "csv") {
            type_blob = "text/csv";
          }
          else if (fileType === "xml") {
            type_blob = "application/xml";
          }
          else if (fileType === "json") {
            type_blob = "application/json";
          }
          else if (fileType === "js") {
            type_blob = "application/javascript";
          }
          else if (fileType === "css") {
            type_blob = "text/css";
          }
          else if (["mp3", "wav", "ogg"].includes(fileType)) {
            type_blob = fileType === "mp3" ? "audio/mpeg" :
              fileType === "wav" ? "audio/wav" :
                "audio/ogg";
          }
          else if (["mp4", "webm", "mov"].includes(fileType)) {
            type_blob = fileType === "mp4" ? "video/mp4" :
              fileType === "webm" ? "video/webm" :
                "video/quicktime";
          }

          const blob = new Blob([fileContent], { type: type_blob });
          const url = window.URL.createObjectURL(blob);
          const newTab = window.open(url, "_blank");

          if (!newTab) {
            console.error("Ouverture bloquée par le navigateur");
            window.URL.revokeObjectURL(url);
          }
        } else {
          console.error("Type de fichier non supporté :", fileType);
        }
      } catch (error) {
        console.error("Erreur lors de l'ouverture du fichier:", error);

        if (error.message && error.message.includes("déchiffrement")) {
          alert("Erreur de déchiffrement. Le mot de passe est peut-être incorrect.");
          pbkdf2CryptoService.clearUserPassword();
        } else {
          alert("Erreur lors de l'ouverture du fichier.");
        }
      }
    },

    getFileType(fileName) {
      const parts = fileName.split(".");
      if (parts.length > 1) {
        return parts[parts.length - 1].toLowerCase();
      }
      return "";
    },

    displayTextFile(textData, openInNewWindow = false) {
      if (process.client) {
        if (openInNewWindow) {
          const newTab = window.open();
          newTab.document.write("<pre>" + textData + "</pre>");
        }
      }
    },

    // Méthode pour afficher le modal des métadonnées
    showFileMetadataModal(event) {
      const fileInput = this.$refs.fileInput;
      if (fileInput.files.length === 0) {
        return;
      }

      this.selectedFile = fileInput.files[0];

      // Initialiser les métadonnées avec le nom du fichier
      this.fileMetadata = {
        displayName: this.selectedFile.name,
        description: "",
        tags: ""
      };

      this.showMetadataModal = true;
    },

    // Méthode pour fermer le modal des métadonnées
    closeMetadataModal() {
      this.showMetadataModal = false;
      this.selectedFile = null;
      this.fileMetadata = {
        displayName: "",
        description: "",
        tags: ""
      };

      // Réinitialiser l'input file
      this.$refs.fileInput.value = "";
    },

    // Méthode pour téléverser un fichier avec métadonnées
    async uploadFileWithMetadata() {
      if (!this.selectedFile) {
        alert("Aucun fichier sélectionné.");
        return;
      }

      const jwtToken = this.getJwtToken();
      let fileToUpload;
      let salt = null;
      let iv = null;
      let encryptedMetadata = null;
      let metadataSalt = null;
      let metadataIV = null;

      try {
        if (!this.isPublic) {
          if (!pbkdf2CryptoService.hasUserPassword()) {
            const password = await this.promptForPassword();
            if (!password) {
              alert("Mot de passe requis pour chiffrer le fichier.");
              return;
            }
            pbkdf2CryptoService.setUserPassword(password);
          }

          // Chiffrer le fichier
          const encryptionResult = await pbkdf2CryptoService.encryptFile(this.selectedFile);
          fileToUpload = encryptionResult.encryptedFile;
          salt = encryptionResult.salt;
          iv = encryptionResult.iv;

          // Chiffrer les métadonnées
          const metadataResult = await pbkdf2CryptoService.encryptMetadata({
            displayName: this.fileMetadata.displayName,
            description: this.fileMetadata.description,
            tags: this.fileMetadata.tags.split(',').map(tag => tag.trim()).filter(tag => tag),
            originalName: this.selectedFile.name,
            dateAdded: new Date().toISOString()
          });

          encryptedMetadata = metadataResult.encryptedMetadata;
          metadataSalt = metadataResult.metadataSalt;
          metadataIV = metadataResult.metadataIV;
        } else {
          fileToUpload = this.selectedFile;
        }

        const formData = new FormData();
        formData.append("file", fileToUpload, this.selectedFile.name);
        formData.append("isPublic", this.isPublic);
        formData.append("userAddress", "user-address");
        formData.append("salt", salt);
        formData.append("iv", iv);
        formData.append("folderId", this.currentFolderId);

        // Ajouter les métadonnées chiffrées si disponibles
        if (encryptedMetadata) {
          formData.append("encryptedMetadata", encryptedMetadata);
          formData.append("metadataSalt", metadataSalt);
          formData.append("metadataIV", metadataIV);
        }

        await axios.post(`${BASE_URL}/api/Files/upload`, formData, {
          headers: {
            Authorization: `Bearer ${jwtToken}`,
            "Content-Type": "multipart/form-data",
          },
        });

        this.closeMetadataModal();
        this.loadFileList(this.currentFolderId);
      } catch (error) {
        console.error("Erreur lors de l'upload:", error);
        alert("Erreur lors de l'envoi du fichier.");
      }
    },

    getJwtToken() {
      const jwtToken = localStorage.getItem("access_token");

      if (!jwtToken) {
        console.error("Le jeton JWT n'est pas disponible.");
        this.$router.push("/");
        return;
      }
      return jwtToken;
    },

    isFilePublic(fileName) {
      const file = this.fileList.find(f => f.name === fileName);
      return file ? file.isPublic : false;
    },

    async promptForPassword() {
      const storedPassword = await pbkdf2CryptoService.retrievePassword();
      if (storedPassword) {
        return storedPassword;
      }

      return new Promise((resolve) => {
        const password = prompt("Veuillez entrer votre mot de passe pour chiffrer/déchiffrer le fichier:");

        if (password) {
          const rememberPassword = confirm("Souhaitez-vous mémoriser ce mot de passe pour cette session?");
          if (rememberPassword) {
            pbkdf2CryptoService.setUserPassword(password);
            pbkdf2CryptoService.rememberPassword(true);
          }
        }

        resolve(password);
      });
    }
  },
};
</script>
