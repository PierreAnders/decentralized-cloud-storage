<template>
  <div class="min-h-screen px-8 pt-8">
    <BurgerMenu />
    <div class="flex items-center justify-center pt-8">
      <h1 class="pr-3 tracking-wider text-light-gray">NOTES</h1>
      <IconNote :color="'#334155'" />
    </div>

    <div class="flex flex-col items-center justify-center mt-10">
      <!-- Sélection du dossier -->
      <div class="w-3/4 mb-4 md:w-2/3 lg:w-1/2">
        <label class="block text-sm text-light-gray mb-1">Dossier de destination</label>
        <select v-model="selectedFolderId" class="w-full p-2 text-white bg-black border border-gray-700 rounded"
          @change="updateFolderPath">
          <option value="">-- Sélectionnez un dossier --</option>
          <option v-for="folder in availableFolders" :key="folder.id" :value="folder.id">
            {{ getFolderPath(folder) }}
          </option>
        </select>
      </div>

      <!-- Titre de la note -->
      <input
        class="w-56 w-3/4 h-8 px-4 mb-4 mx-auto text-sm text-white border-2 rounded-md bg-dark-gray placeholder-light-gray border-dark-gray focus:outline-none focus:border-blue"
        type="text" v-model="note.title" placeholder="Titre de la note" @keyup.enter="saveContent">

      <!-- Barre d'outils de l'éditeur -->
      <div v-if="editor" class="toolbar">
        <button class="m-0.5" @click="editor.chain().focus().toggleBold().run()"
          :class="{ 'is-active': editor.isActive('bold') }">
          <svg class="w-5 h-5 text-light-gray" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" width="24"
            height="24" fill="none" viewBox="0 0 24 24">
            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M8 5h4.5a3.5 3.5 0 1 1 0 7H8m0-7v7m0-7H6m2 7h6.5a3.5 3.5 0 1 1 0 7H8m0-7v7m0 0H6" />
          </svg>
        </button>
        <button class="m-0.5" @click="editor.chain().focus().toggleItalic().run()"
          :class="{ 'is-active': editor.isActive('italic') }">
          <svg class="w-5 h-5 text-light-gray" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" width="24"
            height="24" fill="none" viewBox="0 0 24 24">
            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="m8.874 19 6.143-14M6 19h6.33m-.66-14H18" />
          </svg>
        </button>
        <button class="m-1" @click="editor.chain().focus().toggleStrike().run()"
          :class="{ 'is-active': editor.isActive('strike') }">
          <svg class="w-5 h-5 text-light-gray" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" width="24"
            height="24" fill="none" viewBox="0 0 24 24">
            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M7 6.2V5h12v1.2M7 19h6m.2-14-1.677 6.523M9.6 19l1.029-4M5 5l6.523 6.523M19 19l-7.477-7.477" />
          </svg>
        </button>
        <button class="m-0.5" @click="editor.chain().focus().toggleCode().run()"
          :class="{ 'is-active': editor.isActive('code') }">
          <svg class="w-5 h-5 text-light-gray" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" width="24"
            height="24" fill="none" viewBox="0 0 24 24">
            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="m8 8-4 4 4 4m8 0 4-4-4-4m-2-3-4 14" />
          </svg>
        </button>
        <button class="m-0.5" @click="editor.chain().focus().toggleHighlight().run()"
          :class="{ 'is-active': editor.isActive('highlight') }">
          <svg class="w-5 h-5 text-light-gray" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" width="24"
            height="24" fill="none" viewBox="0 0 24 24">
            <path stroke="currentColor" stroke-linecap="round" stroke-width="2"
              d="M5 19h14M7.6 16l4.2979-10.92963c.0368-.09379.1674-.09379.2042 0L16.4 16m-8.8 0H6.5m1.1 0h1.65m7.15 0h-1.65m1.65 0h1.1m-8.33315-4h5.66025" />
          </svg>
        </button>
        <button class="m-0.5" @click="setLink">
          <svg class="w-5 h-5 text-light-gray" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" width="24"
            height="24" fill="none" viewBox="0 0 24 24">
            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M13.213 9.787a3.391 3.391 0 0 0-4.795 0l-3.425 3.426a3.39 3.39 0 0 0 4.795 4.794l.321-.304m-.321-4.49a3.39 3.39 0 0 0 4.795 0l3.424-3.426a3.39 3.39 0 0 0-4.794-4.795l-1.028.961" />
          </svg>
        </button>
        <button class="m-0.5" @click="editor.chain().focus().toggleTaskList().run()"
          :class="{ 'is-active': editor.isActive('taskList') }">
          <svg class="w-5 h-5 text-light-gray" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" width="24"
            height="24" fill="none" viewBox="0 0 24 24">
            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M8.5 11.5 11 14l4-4m6 2a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" />
          </svg>
        </button>
        <button class="m-0.5"
          @click="editor.chain().focus().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run()">
          <svg class="w-5 h-5 text-light-gray" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" width="24"
            height="24" fill="none" viewBox="0 0 24 24">
            <path stroke="currentColor" stroke-width="2"
              d="M3 11h18M3 15h18m-9-4v8m-8 0h16a1 1 0 0 0 1-1V6a1 1 0 0 0-1-1H4a1 1 0 0 0-1 1v12a1 1 0 0 0 1 1Z" />
          </svg>
        </button>
        <button class="m-0.5" @click="editor.chain().focus().setTextAlign('left').run()"
          :class="{ 'is-active': editor.isActive({ textAlign: 'left' }) }">
          <svg class="w-5 h-5 text-light-gray" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" width="24"
            height="24" fill="none" viewBox="0 0 24 24">
            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M6 6h8m-8 4h12M6 14h8m-8 4h12" />
          </svg>
        </button>
        <button class="m-0.5" @click="editor.chain().focus().setTextAlign('center').run()"
          :class="{ 'is-active': editor.isActive({ textAlign: 'center' }) }">
          <svg class="w-5 h-5 text-light-gray" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" width="24"
            height="24" fill="none" viewBox="0 0 24 24">
            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M8 6h8M6 10h12M8 14h8M6 18h12" />
          </svg>
        </button>
        <button class="m-0.5" @click="editor.chain().focus().setTextAlign('right').run()"
          :class="{ 'is-active': editor.isActive({ textAlign: 'right' }) }">
          <svg class="w-5 h-5 text-light-gray" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" width="24"
            height="24" fill="none" viewBox="0 0 24 24">
            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M18 6h-8m8 4H6m12 4h-8m8 4H6" />
          </svg>
        </button>
        <button class="m-0.5" @click="editor.chain().focus().setTextAlign('justify').run()"
          :class="{ 'is-active': editor.isActive({ textAlign: 'justify' }) }">
          <svg class="w-5 h-5 text-light-gray" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" width="24"
            height="24" fill="none" viewBox="0 0 24 24">
            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M18 6H6m12 4H6m12 4H6m12 4H6" />
          </svg>
        </button>
        <button class="m-0.5" @click="editor.chain().focus().toggleHeading({ level: 2 }).run()"
          :class="{ 'is-active': editor.isActive('heading', { level: 2 }) }">
          <svg class="w-5 h-5 text-light-gray" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" width="24"
            height="24" fill="none" viewBox="0 0 24 24">
            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M3 6.2V5h11v1.2M8 5v14m-3 0h6m2-6.8V11h8v1.2M17 11v8m-1.5 0h3" />
          </svg>
        </button>
        <button class="m-0.5" @click="toggleFullscreen">
          <svg class="w-5 h-5 text-light-gray" aria-hidden="true" xmlns="http://www.w3.org/2000/svg" width="24"
            height="24" fill="none" viewBox="0 0 24 24">
            <path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M8 4H4m0 0v4m0-4 5 5m7-5h4m0 0v4m0-4-5 5M8 20H4m0 0v-4m0 4 5-5m7 5h4m0 0v-4m0 4-5-5" />
          </svg>
        </button>
      </div>

      <!-- Compteur de caractères -->
      <div v-if="editor" class="character-count">
        Nombre de caractères : {{ editor.storage.characterCount.characters() }} / 1000
      </div>

      <!-- Éditeur -->
      <div class="editor-container">
        <editor-content class="markdown-body rounded-xl p-6" :editor="editor" />
      </div>

      <!-- Options de sauvegarde -->
      <div class="flex items-center justify-center mt-4 space-x-4">
        <div class="flex items-center">
          <input type="checkbox" id="isPublic" v-model="note.isPublic" class="mr-2">
          <label for="isPublic" class="text-sm text-light-gray">Rendre public</label>
        </div>

        <button class="px-4 py-2 text-white bg-blue-600 rounded hover:bg-blue-500" @click="saveContent"
          :disabled="!selectedFolderId || !note.title.trim()">
          <IconSave class="transition-transform transform hover:scale-110" />
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { useEditor, EditorContent } from '@tiptap/vue-3';
import StarterKit from '@tiptap/starter-kit';
import 'github-markdown-css/github-markdown-light.css';
import Placeholder from '@tiptap/extension-placeholder';
import Link from '@tiptap/extension-link';
import Image from '@tiptap/extension-image';
import Table from '@tiptap/extension-table';
import TableRow from '@tiptap/extension-table-row';
import TableCell from '@tiptap/extension-table-cell';
import TableHeader from '@tiptap/extension-table-header';
import Code from '@tiptap/extension-code';
import TaskList from '@tiptap/extension-task-list';
import TaskItem from '@tiptap/extension-task-item';
import Highlight from '@tiptap/extension-highlight';
import TextAlign from '@tiptap/extension-text-align';
import CharacterCount from '@tiptap/extension-character-count';
import ImageResize from 'tiptap-extension-resize-image';
import Heading from '@tiptap/extension-heading';
import axios from "axios";
import pbkdf2CryptoService from "../../pbkdf2CryptoService.js";

const BASE_URL = import.meta.env.VITE_BASE_URL;

// États
const note = ref({
  title: '',
  content: '',
  isPublic: false
});
const selectedFolderId = ref('');
const availableFolders = ref([]);
const folderPath = ref([]);

onMounted(() => {
  if (process.client) {
    // Charger le brouillon existant
    const draftContent = localStorage.getItem('draftContent');
    if (draftContent && editor.value) {
      editor.value.commands.setContent(draftContent);
    }

    // Configurer le watcher pour sauvegarder automatiquement
    watch(
      () => editor.value?.getHTML(),
      (newContent) => {
        localStorage.setItem('draftContent', newContent);
      },
      { deep: true }
    );
  }

  // Charger les dossiers
  loadFolders();
});

// Initialisation de l'éditeur
const editor = useEditor({
  content: process.client ? localStorage.getItem('draftContent') || '' : '',
  extensions: [
    StarterKit,
    Heading.configure({
      levels: [1, 2, 3],
      HTMLAttributes: {
        class: 'font-bold my-4',
      },
    }),
    Placeholder.configure({
      placeholder: 'Commencez à écrire...',
    }),
    Link.configure({
      openOnClick: false,
    }),
    Image,
    ImageResize,
    Code,
    Table.configure({
      resizable: true,
    }),
    TableRow,
    TableCell,
    TableHeader,
    TaskList,
    TaskItem,
    Highlight.configure({ color: '#FFC107' }),
    TextAlign.configure({
      types: ['heading', 'paragraph'],
    }),
    CharacterCount.configure({
      limit: 1000,
    }),
  ],
  editorProps: {
    handleKeyDown: (view, event) => {
      if ((event.ctrlKey || event.metaKey) && event.key === 's') {
        event.preventDefault();
        saveContent();
        return true;
      }
      return false;
    },
  },
});

// Chargement des dossiers
const loadFolders = async () => {
  try {
    const jwtToken = getJwtToken();
    const response = await axios.get(`${BASE_URL}/api/Folders`, {
      headers: { Authorization: `Bearer ${jwtToken}` }
    });
    availableFolders.value = response.data;
  } catch (error) {
    console.error("Erreur chargement dossiers:", error);
  }
};

// Mise à jour du chemin du dossier sélectionné
const updateFolderPath = () => {
  if (!selectedFolderId.value) {
    folderPath.value = [];
    return;
  }

  const selectedFolder = availableFolders.value.find(f => f.id === selectedFolderId.value);
  if (selectedFolder) {
    // Construire le chemin complet du dossier
    const path = [];
    let currentFolder = selectedFolder;

    while (currentFolder) {
      path.unshift(currentFolder.name);
      currentFolder = availableFolders.value.find(f => f.id === currentFolder.parentId);
    }

    folderPath.value = path;
  }
};

// Obtenir le chemin complet d'un dossier
const getFolderPath = (folder) => {
  if (!folder.parentId) return folder.name;
  const parent = availableFolders.value.find(f => f.id === folder.parentId);
  return parent ? `${getFolderPath(parent)} / ${folder.name}` : folder.name;
};

// Sauvegarde du contenu
const saveContent = async () => {
  if (!editor.value || !selectedFolderId.value) return;

  try {
    const jwtToken = getJwtToken();
    if (!jwtToken) return;

    const htmlContent = editor.value.getHTML();
    const fileName = `${note.value.title.trim()}.html`;

    let formData = new FormData();
    let fileToUpload;
    let salt = null;
    let iv = null;
    let encryptedMetadata = null;
    let metadataSalt = null;
    let metadataIV = null;

    if (!note.value.isPublic) {
      if (!pbkdf2CryptoService.hasUserPassword()) {
        const password = await promptForPassword();
        if (!password) {
          alert("Mot de passe requis pour chiffrer la note.");
          return;
        }
        pbkdf2CryptoService.setUserPassword(password);
      }

      const blob = new Blob([htmlContent], { type: 'text/html' });
      const file = new File([blob], fileName, { type: 'text/html' });

      const encryptionResult = await pbkdf2CryptoService.encryptFile(file);
      fileToUpload = encryptionResult.encryptedFile;
      salt = encryptionResult.salt;
      iv = encryptionResult.iv;

      const metadataResult = await pbkdf2CryptoService.encryptMetadata({
        displayName: note.value.title.trim(),
        description: "Note créée avec l'éditeur",
        tags: "note,editeur",
        originalName: fileName,
        dateAdded: new Date().toISOString()
      });

      encryptedMetadata = metadataResult.encryptedMetadata;
      metadataSalt = metadataResult.metadataSalt;
      metadataIV = metadataResult.metadataIV;
    } else {
      fileToUpload = new Blob([htmlContent], { type: 'text/html' });
    }

    formData.append("file", fileToUpload, fileName);
    formData.append("category", 'note');
    formData.append("isPublic", note.value.isPublic);
    formData.append("userAddress", 'user-address');
    formData.append("folderId", selectedFolderId.value);

    if (!note.value.isPublic) {
      formData.append("salt", salt);
      formData.append("iv", iv);
      formData.append("encryptedMetadata", encryptedMetadata);
      formData.append("metadataSalt", metadataSalt);
      formData.append("metadataIV", metadataIV);
    }

    const response = await axios.post(`${BASE_URL}/api/Files/upload`, formData, {
      headers: {
        Authorization: `Bearer ${jwtToken}`,
        'Content-Type': 'multipart/form-data',
      },
    });

    alert('Note sauvegardée avec succès!');
    console.log('Réponse:', response.data);

    // Nettoyer le brouillon après sauvegarde
    if (process.client) {
      localStorage.removeItem('draftContent');
    }
  } catch (error) {
    console.error('Erreur:', error);
    alert('Échec de la sauvegarde: ' + error.message);
  }
};

// Utilitaires
const getJwtToken = () => {
  const jwtToken = localStorage.getItem("access_token");
  if (!jwtToken) {
    console.error("Le jeton JWT n'est pas disponible.");
    return null;
  }
  return jwtToken;
};

const promptForPassword = () => {
  return prompt("Veuillez entrer votre mot de passe pour chiffrer la note:");
};

// Fonction pour insérer un lien
const setLink = () => {
  const url = prompt('Entrez l\'URL du lien');
  if (url) {
    editor.value.chain().focus().setLink({ href: url }).run();
  }
};

// Fonction pour téléverser une image
const handleImageUpload = (event) => {
  const file = event.target.files[0];
  if (file) {
    const reader = new FileReader();
    reader.onload = (e) => {
      editor.value.chain().focus().insertImage({ src: e.target.result }).run();
    };
    reader.readAsDataURL(file);
  }
};

// Fonction pour basculer en mode plein écran
const toggleFullscreen = () => {
  const editorElement = document.querySelector('.ProseMirror');
  if (!document.fullscreenElement) {
    if (editorElement.requestFullscreen) {
      editorElement.requestFullscreen();
    } else if (editorElement.webkitRequestFullscreen) {
      editorElement.webkitRequestFullscreen();
    }
  } else {
    if (document.exitFullscreen) {
      document.exitFullscreen();
    } else if (document.webkitExitFullscreen) {
      document.webkitExitFullscreen();
    }
  }
};
</script>

<style>
.markdown-body {
  background-color: #f2f2f2;
}

.toolbar {
  margin-bottom: 10px;
}

.toolbar button {
  margin-right: 5px;
  padding: 5px 10px;
  border: 1px solid #838383;
  border-radius: 4px;
  background-color: #3A3A3A;
  cursor: pointer;
}

.toolbar button.is-active {
  background-color: #646464;
}

.editor-container {
  width: 100%;
  max-width: clamp(300px, 90vw, 800px);
  padding: 10px;
}

.ProseMirror {
  padding: 15px;
  border: 1px solid #f2f2f2;
  border-radius: 8px;
  min-height: 380px;
  margin: 0 auto;
  background-color: #f2f2f2;
  font-family: 'Arial', sans-serif;
  font-size: 16px;
  line-height: 1.6;
  box-sizing: border-box;
}

.ProseMirror:focus {
  outline: none;
}

.character-count {
  margin-top: 10px;
  font-size: 14px;
  color: #666;
}

@media (max-width: 768px) {
  .ProseMirror {
    padding: 10px;
    font-size: 14px;
    min-height: 60vh;
  }

  .toolbar button {
    padding: 3px 6px;
    font-size: 12px;
  }
}
</style>