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
        <div>
          <input type="checkbox" class="mr-2" name="isPublic" id="isPublic" v-model="isPublic" />
          <label for="isPublic" class="text-light-gray">Public</label>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import axios from "axios";
import { useTextContentStore } from "../../textContentStore.js";
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

        const axiosConfig = {
          headers: {
            Authorization: `Bearer ${jwtToken}`,
          },
          responseType: "blob",
        };

        const response = await axios.get(
          `${BASE_URL}/api/Files/decrypt/${fileName}`,
          axiosConfig
        );

        // Création d'un blob (Binary Large OBject, ensemble de données binaires) à partir des données de la réponse
        const blob = new Blob([response.data]);

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
      } catch (error) {
        console.error(error);
      }
    },

    openFile(fileName) {
      const jwtToken = this.getJwtToken();
      const fileType = this.getFileType(fileName);
      const responseType = fileType === "html" ? "text" : "arraybuffer";

      const axiosConfig = {
        headers: { Authorization: `Bearer ${jwtToken}` },
        responseType: responseType,
      };

      axios
        .get(`${BASE_URL}/api/Files/decrypt/${fileName}`, axiosConfig)
        .then((response) => {
          const fileType = this.getFileType(fileName);

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
              this.displayTextFile(response.data, true);
              return;
            }
            else if (fileType === "html") {
              const fileNameWithoutExtension = fileName.replace(/\.[^/.]+$/, "");
              useTextContentStore().setTextContent(response.data);
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

            const blob = new Blob([response.data], { type: type_blob });
            const url = window.URL.createObjectURL(blob);
            const newTab = window.open(url, "_blank");

            if (!newTab) {
              console.error("Ouverture bloquée par le navigateur");
              window.URL.revokeObjectURL(url);
            }
          } else {
            console.error("Type de fichier non supporté :", fileType);
          }
        })
        .catch((error) => console.error(error));
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

    displayTextFile(textData) {
      console.log("Displaying text file content:");
      console.log(textData);
    },

    uploadFile() {
      // Récupération du JWT Token
      const jwtToken = this.getJwtToken();

      // Initialisation de FormData
      const formData = new FormData();

      // Récupération du champ input file via sa référence 'fileInput'
      const fileInput = this.$refs.fileInput;

      // Si aucun fichier n'est sélectionné, on affiche une erreur et on quitte la fonction
      if (fileInput.files.length === 0) {
        console.error("Aucun fichier sélectionné.");
        return;
      }


      formData.append("file", fileInput.files[0]);
      formData.append("category", this.folderName);
      formData.append("isPublic", this.isPublic);
      formData.append("userAddress", "user-address");
      console.log("formData", formData);

      axios
        .post(`${BASE_URL}/api/Files/upload`, formData, {
          headers: {
            Authorization: `Bearer ${jwtToken}`,
            "Content-Type": "multipart/form-data",
          },
        })
        .then(() => {
          // Rafraîssement de la liste des fichiers une fois le fichier téléversé
          this.loadFileList();
          // Vide le champs de l'input
          fileInput.value = "";
        })
        // En cas d'erreur, on l'affiche dans la console
        .catch((error) => console.error(error));
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

    displayTextFile(textData, openInNewWindow = false) {
      // Conversion en UTF-8 avec TextDecoder
      const decoder = new TextDecoder("utf-8");
      const decodedText = decoder.decode(textData);

      if (openInNewWindow) {
        // Ouvrir le contenu dans une nouvelle fenêtre
        const newTab = window.open();
        newTab.document.write("<pre>" + decodedText + "</pre>");
      }
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
  },
};
</script>
