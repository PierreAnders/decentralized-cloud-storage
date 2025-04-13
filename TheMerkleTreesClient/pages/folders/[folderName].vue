<template>
  <div class="min-h-screen px-8 pt-8">
    <BurgerMenu />
    <div class="flex items-center justify-center pt-8">
      <h1 class="pr-3 tracking-wider text-light-gray">
        {{ currentFolder ? currentFolder.name.toUpperCase() : 'RACINE' }}
      </h1>
      <IconFolder />
    </div>

    <!-- Chemin de navigation (breadcrumb) -->
    <div class="flex items-center justify-center mt-4 text-sm text-light-gray">
      <span 
        @click="navigateToRoot" 
        class="cursor-pointer hover:text-white"
      >
        Racine
      </span>
      <span v-for="(folder, index) in breadcrumb" :key="folder.id" class="flex items-center">
        <span class="mx-2">/</span>
        <span 
          @click="navigateToFolder(folder.id)" 
          class="cursor-pointer hover:text-white"
        >
          {{ folder.name }}
        </span>
      </span>
    </div>

    <!-- Liste des sous-dossiers -->
    <div v-if="subfolders.length > 0" class="w-3/4 mx-auto mt-8 md:w-2/3 lg:w-1/2">
      <h2 class="mb-4 text-sm text-light-gray">Dossiers</h2>
      <ul class="flex flex-col">
        <li v-for="folder in subfolders" :key="folder.id" class="flex flex-col justify-between mt-4 text-white md:flex-row">
          <div class="flex justify-between w-full">
            <div class="flex items-center space-x-2 cursor-pointer" @click="navigateToFolder(folder.id)">
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
      <h2 class="mb-4 text-sm text-light-gray">Fichiers</h2>
      <li v-for="file in fileList" :key="file.id" class="flex flex-col justify-between mt-4 text-white md:flex-row">
        <div class="flex justify-between w-full">
          <div class="flex items-center space-x-2">
            <IconDocument :color="'#828282'" iclass="opacity-50" />
            <span class="text-sm">{{ file.name }}</span>
          </div>
          <div class="flex items-center space-x-2">
            <button @click="deleteFile(file.name)">
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
      </li>
    </ul>

    <!-- Actions -->
    <div class="flex justify-center space-x-8">
      <!-- Ajouter un fichier -->
      <div class="flex flex-col justify-center">
        <label for="fileInput" class="text-light-gray">
          <div id="fileNameLabel" class="flex flex-col justify-center cursor-pointer text-light-gray">
            <IconUpload class="pb-2 mx-auto transition-transform transform w-14 hover:scale-110" />
          </div>
          <input type="file" id="fileInput" ref="fileInput"
            accept=".pdf,.doc,.docx,.xls,.xlsx,.txt,.md,.html,.mp3,.mp4,.png,.jpg,.jpeg,.gif,mpeg,.webm,.ogg,.wav,.flac,.mp3,.mp4,.avi,.mov,.wmv,.mkv,.flv,.webm,.ogg,.wav,.flac,.mp3,.mp4,.avi,.mov"
            class="p-2 border rounded-md bg-neutral-300 text-neutral-800 focus:outline-none focus:border-amber-800"
            @change="uploadFile" style="display: none" />
        </label>
        <div class="flex flex-col items-center">
          <h2 class="text-light-gray text-sm">Ajouter un fichier</h2>
          <div class="mt-1">
            <label for="isPublic" class="text-light-gray text-sm mr-2">Public</label>
            <input type="checkbox" class="opacity-90" name="isPublic" id="isPublic" v-model="isPublic" />
          </div>
        </div>
      </div>

      <!-- Créer un dossier -->
      <div class="flex flex-col justify-center">
        <div class="flex flex-col justify-center cursor-pointer text-light-gray" @click="showCreateFolder">
          <IconNewFolder class="pb-2 mx-auto transition-transform transform w-14 hover:scale-110" />
        </div>
        <div class="flex flex-col items-center">
          <h2 class="text-light-gray text-sm">Créer un dossier</h2>
        </div>
      </div>
    </div>

    <!-- Modal pour créer/renommer un dossier -->
    <div v-if="showFolderModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-50">
      <div class="p-6 bg-dark-gray rounded-lg shadow-lg">
        <h2 class="mb-4 text-xl text-white">{{ editingFolder ? 'Renommer le dossier' : 'Créer un dossier' }}</h2>
        <input 
          v-model="folderName" 
          class="w-full p-2 mb-4 text-white bg-black border border-gray-700 rounded"
          placeholder="Nom du dossier"
          @keyup.enter="editingFolder ? updateFolder() : createFolder()"
        />
        <div class="flex justify-between">
          <button 
            @click="closeFolderModal" 
            class="px-4 py-2 text-white bg-gray-700 rounded hover:bg-gray-600"
          >
            Annuler
          </button>
          <button 
            @click="editingFolder ? updateFolder() : createFolder()" 
            class="px-4 py-2 text-white bg-blue-600 rounded hover:bg-blue-500"
          >
            {{ editingFolder ? 'Renommer' : 'Créer' }}
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
      subfolders: [],
      breadcrumb: [],
      currentFolder: null,
      currentFolderId: null,
      isPublic: false,
      showFolderModal: false,
      folderName: "",
      editingFolder: null,
    };
  },
  mounted() {
    // Récupérer l'ID du dossier depuis l'URL si présent
    const folderId = this.$route.query.folderId;
    if (folderId) {
      this.currentFolderId = folderId;
      this.loadFolder(folderId);
    } else {
      this.loadRootContent();
    }
  },
  methods: {
    loadRootContent() {
      this.currentFolder = null;
      this.currentFolderId = null;
      this.breadcrumb = [];
      this.loadFileList();
      this.loadSubfolders();
    },
    loadFolder(folderId) {
      if (process.client) {
        const jwtToken = this.getJwtToken();

        axios
          .get(`${BASE_URL}/api/Folders/${folderId}`, {
            headers: {
              Authorization: `Bearer ${jwtToken}`,
              "Content-Type": "application/json",
            },
          })
          .then((response) => {
            this.currentFolder = response.data;
            this.currentFolderId = folderId;
            this.loadBreadcrumb(this.currentFolder);
            this.loadFileList();
            this.loadSubfolders();
          })
          .catch((error) => {
            console.error("Erreur lors du chargement du dossier:", error);
            this.loadRootContent(); // Retour à la racine en cas d'erreur
          });
      }
    },
    loadBreadcrumb(folder) {
      this.breadcrumb = [];
      this.buildBreadcrumb(folder);
    },
    buildBreadcrumb(folder) {
      if (!folder || !folder.parentId) {
        return;
      }

      const jwtToken = this.getJwtToken();
      
      axios
        .get(`${BASE_URL}/api/Folders/${folder.parentId}`, {
          headers: {
            Authorization: `Bearer ${jwtToken}`,
            "Content-Type": "application/json",
          },
        })
        .then((response) => {
          const parentFolder = response.data;
          this.breadcrumb.unshift(parentFolder);
          this.buildBreadcrumb(parentFolder);
        })
        .catch((error) => {
          console.error("Erreur lors du chargement du chemin de navigation:", error);
        });
    },
    loadFileList() {
      if (process.client) {
        const jwtToken = this.getJwtToken();
        const endpoint = this.currentFolderId 
          ? `${BASE_URL}/api/Files/folder/${this.currentFolderId}`
          : `${BASE_URL}/api/Files/user`;

        axios
          .get(endpoint, {
            headers: {
              Authorization: `Bearer ${jwtToken}`,
              "Content-Type": "application/json",
            },
          })
          .then((response) => {
            this.fileList = response.data;
          })
          .catch((error) => console.error(error));
      } else {
        console.error(
          "Le code est exécuté côté serveur (SSR), localStorage n'est pas disponible."
        );
      }
    },
    loadSubfolders() {
      if (process.client) {
        const jwtToken = this.getJwtToken();
        const endpoint = this.currentFolderId 
          ? `${BASE_URL}/api/Folders/${this.currentFolderId}/subfolders`
          : `${BASE_URL}/api/Folders/root`;

        axios
          .get(endpoint, {
            headers: {
              Authorization: `Bearer ${jwtToken}`,
              "Content-Type": "application/json",
            },
          })
          .then((response) => {
            this.subfolders = response.data;
          })
          .catch((error) => console.error("Erreur lors du chargement des sous-dossiers:", error));
      }
    },
    navigateToRoot() {
      this.loadRootContent();
      this.$router.push({ query: {} });
    },
    navigateToFolder(folderId) {
      this.loadFolder(folderId);
      this.$router.push({ query: { folderId } });
    },
    showCreateFolder() {
      this.editingFolder = null;
      this.folderName = "";
      this.showFolderModal = true;
    },
    showRenameFolder(folder) {
      this.editingFolder = folder;
      this.folderName = folder.name;
      this.showFolderModal = true;
    },
    closeFolderModal() {
      this.showFolderModal = false;
      this.folderName = "";
      this.editingFolder = null;
    },
    createFolder() {
      if (!this.folderName.trim()) {
        alert("Veuillez entrer un nom de dossier");
        return;
      }

      const jwtToken = this.getJwtToken();
      
      const newFolder = {
        id: "test",
        name: this.folderName.trim(),
        parentId: this.currentFolderId,
        isPublic: false,
        owner: "test",

      };
      console.log(newFolder);

      axios
        .post(`${BASE_URL}/api/Folders`, newFolder, {
          headers: {
            Authorization: `Bearer ${jwtToken}`,
            "Content-Type": "application/json",
          },
        })
        .then(() => {
          this.loadSubfolders();
          this.closeFolderModal();
        })
        .catch((error) => {
          console.error("Erreur lors de la création du dossier:", error);
          alert("Erreur lors de la création du dossier");
        });
    },
    updateFolder() {
      if (!this.folderName.trim()) {
        alert("Veuillez entrer un nom de dossier");
        return;
      }

      const jwtToken = this.getJwtToken();
      
      const updateData = {
        name: this.folderName.trim(),
        parentId: this.editingFolder.parentId,
        isPublic: this.editingFolder.isPublic
      };

      axios
        .put(`${BASE_URL}/api/Folders/${this.editingFolder.id}`, updateData, {
          headers: {
            Authorization: `Bearer ${jwtToken}`,
            "Content-Type": "application/json",
          },
        })
        .then(() => {
          this.loadSubfolders();
          this.closeFolderModal();
        })
        .catch((error) => {
          console.error("Erreur lors de la mise à jour du dossier:", error);
          alert("Erreur lors de la mise à jour du dossier");
        });
    },
    deleteFolder(folderId) {
      if (!confirm("Êtes-vous sûr de vouloir supprimer ce dossier et tout son contenu ?")) {
        return;
      }

      const jwtToken = this.getJwtToken();

      axios
        .delete(`${BASE_URL}/api/Folders/${folderId}`, {
          headers: {
            Authorization: `Bearer ${jwtToken}`,
            "Content-Type": "application/json",
          },
        })
        .then(() => {
          this.loadSubfolders();
        })
        .catch((error) => {
          console.error("Erreur lors de la suppression du dossier:", error);
          alert("Erreur lors de la suppression du dossier");
        });
    },
    deleteFile(fileName) {
      const jwtToken = this.getJwtToken();

      axios
        .delete(`${BASE_URL}/api/Files/${fileName}`, {
          headers: {
            Authorization: `Bearer ${jwtToken}`,
            "Content-Type": "application/json",
          },
        })
        .then(() => {
          this.loadFileList();
        })
        .catch((error) => console.error(error));
    },
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
          else if (fileType === "html") {
            const decoder = new TextDecoder("utf-8");
            const decodedText = decoder.decode(fileContent);
            useTextContentStore().setTextContent(decodedText);
            useTextContentStore().setFileNameWithoutExtension(fileName);
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
      if (openInNewWindow) {
        const newTab = window.open();
        newTab.document.write("<pre>" + textData + "</pre>");
      }
    },
    async uploadFile() {
      const jwtToken = this.getJwtToken();
      const fileInput = this.$refs.fileInput;

      if (fileInput.files.length === 0) {
        console.error("Aucun fichier sélectionné.");
        return;
      }

      const file = fileInput.files[0];
      let fileToUpload;
      let salt = null;
      let iv = null;

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

          const encryptionResult = await pbkdf2CryptoService.encryptFile(file);
          fileToUpload = encryptionResult.encryptedFile;
          salt = encryptionResult.salt;
          iv = encryptionResult.iv;
        } else {
          fileToUpload = file;
        }

        const formData = new FormData();
        formData.append("file", fileToUpload, file.name);
        formData.append("category", "files"); // Catégorie par défaut
        formData.append("isPublic", this.isPublic);
        formData.append("userAddress", "user-address");
        formData.append("salt", salt);
        formData.append("iv", iv);
        
        // Ajouter l'ID du dossier si on est dans un dossier
        if (this.currentFolderId) {
          formData.append("folderId", this.currentFolderId);
        }

        await axios.post(`${BASE_URL}/api/Files/upload`, formData, {
          headers: {
            Authorization: `Bearer ${jwtToken}`,
            "Content-Type": "multipart/form-data",
          },
        });

        this.loadFileList();
        fileInput.value = "";
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
