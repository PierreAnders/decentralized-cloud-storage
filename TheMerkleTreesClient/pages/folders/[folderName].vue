<template>
  <div class="min-h-screen px-8 pt-8">
    <BurgerMenu />
    <div class="flex items-center justify-center pt-8">
      <h1 class="pr-3 tracking-wider text-light-gray">
        {{ folderName.toUpperCase() }}
      </h1>
      <IconFolder />
    </div>
    <ul class="flex flex-col w-3/4 mx-auto mt-12 mb-8 md:w-2/3 lg:w-1/2">
      <li v-for="file in fileList" :key="file.id" class="flex flex-col justify-between mt-6 text-white md:flex-row">
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
    <div class="flex justify-center">
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
    </div>
  </div>
</template>

<script>
import axios from "axios";
import { useTextContentStore } from "../../textContentStore.js";
import pbkdf2CryptoService from "../../pbkdf2CryptoService.js"; // Import du service de chiffrement PBKDF2
const BASE_URL = import.meta.env.VITE_BASE_URL;

export default {
  data() {
    return {
      fileList: [],
      folderName: "",
      isPublic: false,
    };
  },
  methods: {
    loadFileList() {
      if (process.client) {
        const jwtToken = this.getJwtToken();

        axios
          .get(`${BASE_URL}/api/Files/user/category/${this.folderName}`, {
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

        // Vérifier si l'utilisateur a défini son mot de passe
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

        // Récupération du fichier chiffré
        const response = await axios.get(
          `${BASE_URL}/api/Files/file/${fileName}`,
          axiosConfig
        );

        // Si le fichier est public, pas besoin de déchiffrement
        if (this.isFilePublic(fileName)) {
          const blob = new Blob([response.data]);
          this.saveBlob(blob, fileName);
          return;
        }

        // Récupération du sel associé au fichier
        const saltResponse = await axios.get(
          `${BASE_URL}/api/Files/salt/${fileName}`,
          {
            headers: {
              Authorization: `Bearer ${jwtToken}`,
            },
          }
        );

        const salt = saltResponse.data.salt;
        
        if (!salt) {
          console.error("Sel de déchiffrement non trouvé pour ce fichier");
          alert("Impossible de déchiffrer ce fichier. Le sel n'est pas disponible.");
          return;
        }

        // Déchiffrement du fichier avec le mot de passe et le sel
        const fileContent = await pbkdf2CryptoService.decryptData(
          response.data,
          salt
        );

        // Création d'un blob à partir des données déchiffrées
        const blob = new Blob([fileContent]);
        this.saveBlob(blob, fileName);
      } catch (error) {
        console.error("Erreur lors du téléchargement:", error);
        
        // Si l'erreur est due à un mauvais mot de passe, demander à nouveau
        if (error.message && error.message.includes("déchiffrement")) {
          alert("Erreur de déchiffrement. Le mot de passe est peut-être incorrect.");
          pbkdf2CryptoService.clearUserPassword();
        } else {
          alert("Erreur lors du téléchargement du fichier.");
        }
      }
    },

    // Fonction utilitaire pour sauvegarder un blob en tant que fichier
    saveBlob(blob, fileName) {
      // Création d'une URL pour le blob
      const url = window.URL.createObjectURL(blob);

      // Création d'un lien pointant vers le blob
      const link = document.createElement("a");
      link.href = url;

      // Ajout de l'attribut 'download' au lien, avec le nom du fichier original
      link.setAttribute("download", fileName);

      // Ajout du lien au corps de la page HTML
      document.body.appendChild(link);

      // Déclenchement du clic sur le lien, ce qui lance le téléchargement du fichier
      link.click();

      // Suppression du lien du corps de la page
      link.remove();

      // Suppression de l'URL créée pour le blob, pour libérer les ressources
      window.URL.revokeObjectURL(url);
    },

    async openFile(fileName) {
      try {
        const jwtToken = this.getJwtToken();
        const fileType = this.getFileType(fileName);
        
        // Vérifier si l'utilisateur a défini son mot de passe
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

        // Récupération du fichier chiffré
        const response = await axios.get(
          `${BASE_URL}/api/Files/file/${fileName}`,
          axiosConfig
        );

        let fileContent;
        
        // Si le fichier est public, pas besoin de déchiffrement
        if (this.isFilePublic(fileName)) {
          fileContent = response.data;
        } else {
          // Récupération du sel associé au fichier
          const saltResponse = await axios.get(
            `${BASE_URL}/api/Files/salt/${fileName}`,
            {
              headers: {
                Authorization: `Bearer ${jwtToken}`,
              },
            }
          );

          const salt = saltResponse.data.salt;
          
          if (!salt) {
            console.error("Sel de déchiffrement non trouvé pour ce fichier");
            alert("Impossible de déchiffrer ce fichier. Le sel n'est pas disponible.");
            return;
          }

          // Déchiffrement du fichier avec le mot de passe et le sel
          fileContent = await pbkdf2CryptoService.decryptData(
            response.data,
            salt
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
            // Pour les fichiers texte, on utilise TextDecoder pour les afficher
            const decoder = new TextDecoder("utf-8");
            const decodedText = decoder.decode(fileContent);
            this.displayTextFile(decodedText, true);
            return;
          }
          else if (fileType === "html") {
            // Pour les fichiers HTML, on utilise TextDecoder et on les affiche dans la page de note
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
        
        // Si l'erreur est due à un mauvais mot de passe, demander à nouveau
        if (error.message && error.message.includes("déchiffrement")) {
          alert("Erreur de déchiffrement. Le mot de passe est peut-être incorrect.");
          pbkdf2CryptoService.clearUserPassword();
        } else {
          alert("Erreur lors de l'ouverture du fichier.");
        }
      }
    },

    getFileType(fileName) {
      // Extraire l'extension du fichier
      const parts = fileName.split(".");

      // Vérification si le fichier a plusieurs extension et retourne la dernière
      if (parts.length > 1) {
        return parts[parts.length - 1].toLowerCase();
      }
      return "";
    },

    displayTextFile(textData, openInNewWindow = false) {
      if (openInNewWindow) {
        // Ouvrir le contenu dans une nouvelle fenêtre
        const newTab = window.open();
        newTab.document.write("<pre>" + textData + "</pre>");
      }
    },

    async uploadFile() {
      // Récupération du JWT Token
      const jwtToken = this.getJwtToken();

      // Récupération du champ input file via sa référence 'fileInput'
      const fileInput = this.$refs.fileInput;

      // Si aucun fichier n'est sélectionné, on affiche une erreur et on quitte la fonction
      if (fileInput.files.length === 0) {
        console.error("Aucun fichier sélectionné.");
        return;
      }

      const file = fileInput.files[0];
      let fileToUpload;
      let salt = null;

      try {
        // Si le fichier n'est pas public, on le chiffre avant l'envoi
        if (!this.isPublic) {
          // Vérifier si l'utilisateur a défini son mot de passe
          if (!pbkdf2CryptoService.hasUserPassword()) {
            const password = await this.promptForPassword();
            if (!password) {
              alert("Mot de passe requis pour chiffrer le fichier.");
              return;
            }
            pbkdf2CryptoService.setUserPassword(password);
          }
          
          // Chiffrement du fichier avec PBKDF2
          const encryptionResult = await pbkdf2CryptoService.encryptFile(file);
          fileToUpload = encryptionResult.encryptedFile;
          salt = encryptionResult.salt;
        } else {
          // Si le fichier est public, on l'envoie tel quel
          fileToUpload = file;
        }

        // Initialisation de FormData
        const formData = new FormData();
        formData.append("file", fileToUpload, file.name); // On conserve le nom original du fichier
        formData.append("category", this.folderName);
        formData.append("isPublic", this.isPublic);
        formData.append("userAddress", "user-address");
        formData.append("salt", salt); // Ajout du sel pour les fichiers chiffrés

        // Envoi du fichier au serveur
        await axios.post(`${BASE_URL}/api/Files/upload`, formData, {
          headers: {
            Authorization: `Bearer ${jwtToken}`,
            "Content-Type": "multipart/form-data",
          },
        });

        // Rafraîssement de la liste des fichiers une fois le fichier téléversé
        this.loadFileList();
        // Vide le champs de l'input
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

    // Vérifie si un fichier est public en cherchant dans la liste des fichiers
    isFilePublic(fileName) {
      const file = this.fileList.find(f => f.name === fileName);
      return file ? file.isPublic : false;
    },

    // Demande le mot de passe à l'utilisateur
    async promptForPassword() {
      // Vérifier d'abord si le mot de passe est stocké dans la session
      const storedPassword = pbkdf2CryptoService.retrievePassword();
      if (storedPassword) {
        return storedPassword;
      }
      
      // Sinon, demander le mot de passe à l'utilisateur
      return new Promise((resolve) => {
        const password = prompt("Veuillez entrer votre mot de passe pour chiffrer/déchiffrer le fichier:");
        
        if (password) {
          // Demander si l'utilisateur souhaite se souvenir du mot de passe pour cette session
          const rememberPassword = confirm("Souhaitez-vous mémoriser ce mot de passe pour cette session?");
          if (rememberPassword) {
            pbkdf2CryptoService.setUserPassword(password);
            pbkdf2CryptoService.rememberPassword(true);
          }
        }
        
        resolve(password);
      });
    },
  },

  setup() {
    definePageMeta({
      middleware: ["auth"],
    });
  },

  created() {
    this.folderName = this.$route.params.folderName;
    this.loadFileList();
    
    // Tenter de récupérer le mot de passe stocké dans la session
    const storedPassword = pbkdf2CryptoService.retrievePassword();
    if (storedPassword) {
      pbkdf2CryptoService.setUserPassword(storedPassword);
    }
  },
};
</script>
