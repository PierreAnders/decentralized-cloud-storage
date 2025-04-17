<template>
    <div class="z-10 min-h-screen px-8 pt-8">
        <BurgerMenu />
        <div class="flex items-center justify-center pt-8">
            <h1 class="pr-3 tracking-wider text-light-gray">DOCUMENTS</h1>
            <IconDocument :color="'#334155'" />
        </div>
        <div class="flex flex-col items-center justify-center mt-12">
            <div class="mt-6 mb-24 text-white">
                <div class="grid grid-cols-2 gap-10 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
                    <div v-for="folder in folders" :key="folder.id" class="text-center"
                        @touchstart="touchStart($event, folder.id)" @touchend="touchEnd($event)"
                        @contextmenu.prevent="showContextMenu($event, folder.id)">
                        <button @click="navigateToFolder(folder.id)"
                            class="flex flex-col items-center transition-transform transform hover:scale-105">
                            <ImageFolder class="mx-auto mb-1 w-22 h-22" />
                            <span class="text-xs">{{ folder.name }}</span>
                        </button>
                    </div>
                    <form @submit.prevent="addFolder" class="flex flex-col items-center">
                        <div>
                            <IconAddFolder class="transition-transform transform cursor-pointer hover:scale-105"
                                id="button-add-folder" @click="isButtonClicked = !isButtonClicked"
                                v-show="!isButtonClicked" />
                        </div>
                        <h2 class="text-light-gray text-sm mt-2" v-show="!isButtonClicked">Ajouter</h2>
                        <div id="icon-add-folder" v-show="isButtonClicked">
                            <div class="flex flex-col items-center">
                                <ImageFolder class="pb-[2px]" />
                                <input
                                    class="text-center text-xs w-[90px] h-5 px-2 bg-black border rounded-sm border-dark-gray text-white"
                                    type="text" id="folder" v-model="folderInfo.name" placeholder="Nouveau">
                                <button class="pt-2 transition-transform transform hover:scale-110" type="submit">
                                    <IconSubmenuAddFolder />
                                </button>
                            </div>
                        </div>
                    </form>
                    <div>
                        <div v-show="contextMenu.isVisible" class="context-menu" ref="subMenu"
                            :style="{ left: contextMenu.x + 'px', top: contextMenu.y + 'px' }">
                            <div class="flex flex-col w-48 text-left">

                                <button
                                    class="flex text-left text-sm py-1 rounded-sm hover:bg-[#D9D9D9] hover:bg-opacity-25"
                                    @click="deleteFolder(contextMenu.folderId)">
                                    <div class="px-2">
                                        <IconSubmenuDeleteFolder class="w-5 h-5" :color="'#838383'" />
                                    </div>
                                    <div>
                                        Supprimer le dossier
                                    </div>
                                </button>
                                <button
                                    class="flex text-left text-sm py-1 rounded-sm hover:bg-[#D9D9D9] hover:bg-opacity-[12%]"
                                    @click="isButtonClicked = true">
                                    <div class="px-2">
                                        <IconSubmenuAddFolder class="w-5 h-5" />
                                    </div>
                                    <div>
                                        Nouveau Dossier
                                    </div>
                                </button>
                                <button
                                    class="flex text-left text-sm py-1 rounded-sm hover:bg-[#D9D9D9] hover:bg-opacity-[12%]"
                                    @click="contextMenu.isVisible = false">
                                    <div class="px-2">
                                        <IconSubmenuOut class="w-5 h-5" />
                                    </div>
                                    <div>
                                        Retour
                                    </div>
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>
    
<script>
import axios from 'axios'
const BASE_URL = import.meta.env.VITE_BASE_URL

export default {
    data() {
        return {
            folderInfo: {
                id: "",
                name: "",
                owner: "",
            },
            folders: [],
            isButtonClicked: false,
            contextMenu: {
                isVisible: false,
                folderId: null, // Changé de folderName à folderId
                x: 0,
                y: 0
            },
        };
    },

    methods: {
        // Gestion du menu contextuel pour le mobile en restant longtemps appuyé sur un dossier
        touchStart(event, folderId) {
            // Si le toucher est maintenu pendant 700ms
            // On affiche le menu contextuel
            this.touchTimeout = setTimeout(() => this.showContextMenu(event, folderId), 700)
        },
        // Sinon, on annule l'affichage du menu contextuel
        touchEnd() {
            clearTimeout(this.touchTimeout)
        },
        
        handleClickOutside(event) {
            // Si le click est en dehors du menu
            if (this.$refs.subMenu && !this.$refs.subMenu.contains(event.target)) {
                // Fermer le menu
                this.contextMenu.isVisible = false
            }
        },

        showContextMenu(event, folderId) {
            event.preventDefault()
            // Enregistrer l'ID du dossier sélectionné
            this.contextMenu.folderId = folderId

            // Vérifier si l'option "Supprimer" a été sélectionnée
            if (event.target.textContent === "Supprimer") {
                this.deleteFolder(folderId)
            } else {
                // clientX : Coordonnée X du pointeur de la souris
                this.contextMenu.x = event.clientX
                // clientY : Coordonnée Y du pointeur de la souris
                this.contextMenu.y = event.clientY
                // Afficher le menu contextuel
                this.contextMenu.isVisible = true
            }
        },

        async addFolder() {
            this.contextMenu.isVisible = false
            this.isButtonClicked = false

            if (!this.folderInfo.name) {
                console.error("Le nom du dossier ne peut pas être vide.")
                return;
            }
            try {
                const token = this.getJwtToken()

                const headers = {
                    Authorization: `Bearer ${token}`,
                    'Content-Type': 'application/json'
                };

                // Créer un dossier racine avec la nouvelle API
                const newFolder = {
                    name: this.folderInfo.name,
                    parentId: null, // Dossier racine
                    isPublic: false
                };

                const response = await axios.post(`${BASE_URL}/api/Folders`, newFolder, { headers });

                if (response.status === 201) {
                    console.log("Nouveau dossier créé avec succès.")
                } else {
                    console.error("Échec de la création du dossier.")
                }

            } catch (error) {
                console.error("Erreur lors de la création du dossier:", error)
            }
            this.getAllFolders()
            this.resetFolderInfo()
        },

        resetFolderInfo() {
            this.folderInfo.name = "";
        },

        async getAllFolders() {
            try {
                const token = this.getJwtToken()

                const headers = {
                    Authorization: `Bearer ${token}`
                };

                // Utiliser la nouvelle API pour récupérer les dossiers racine
                const response = await axios.get(`${BASE_URL}/api/Folders/root`, { headers });

                if (response.status === 200) {
                    this.folders = response.data;
                    this.folders.sort((a, b) => {
                        const nameA = a.name.toUpperCase()
                        const nameB = b.name.toUpperCase()

                        if (nameA < nameB) {
                            return -1;
                        }
                        if (nameA > nameB) {
                            return 1;
                        }
                        return 0;
                    });
                } else {
                    console.error("Échec de la récupération des dossiers.")
                }
            } catch (error) {
                console.error("Erreur lors de la récupération des dossiers :", error)
            }
        },

        async deleteFolder(folderId) {
            try {
                const token = this.getJwtToken()

                const headers = {
                    Authorization: `Bearer ${token}`
                };

                // Supprimer le dossier avec la nouvelle API
                const response = await axios.delete(`${BASE_URL}/api/Folders/${folderId}`, { headers });

                if (response.status === 204) {
                    console.log("Dossier supprimé avec succès.")
                    this.getAllFolders()
                    this.contextMenu.isVisible = false
                } else {
                    console.error("Échec de la suppression du dossier.")
                }
            } catch (error) {
                console.error("Erreur lors de la suppression du dossier :", error);
            }
        },

        navigateToFolder(folderId) {
            // Trouver le nom du dossier à partir de son ID pour conserver la compatibilité avec les routes
            const folder = this.folders.find(f => f.id === folderId);
            if (folder) {
                this.$router.push(`/folders/${folder.name}?folderId=${folderId}`);
            }
        },

        getJwtToken() {
            const jwtToken = localStorage.getItem('access_token')

            if (!jwtToken) {
                console.error('Le jeton JWT n\'est pas disponible.')
                this.$router.push('/')
                return;
            }
            return jwtToken;
        },
    },

    setup() {
        definePageMeta({
            middleware: ['auth'],
        });
    },

    mounted() {
        this.getAllFolders()
        document.addEventListener("click", this.handleClickOutside)
    },
};
</script>
    
<style scoped>
.context-menu {
    position: absolute;
    left: 50%;
    top: 50%;
    transform: translate(-50%, -50%);
    z-index: 10;
    background-color: #3A3A3A;
    padding: 5px;
    border-radius: 5px;
}
</style>
