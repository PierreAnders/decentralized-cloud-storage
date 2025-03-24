<template>
    <div class="min-h-screen px-8 pt-8 pb-16">
        <BurgerMenu />
        <div class="flex items-center justify-center pt-8">
            <h1 class="pr-3 tracking-wider text-light-gray">HOME</h1>
            <IconHome :color="'#334155'" class="mr-2" />
        </div>
        <Datetime class="pt-12" />
        <div class="mt-12 w-2/3 sm:w-2/3 md:w-1/2 mx-auto">
            <div class="grid grid-cols-3 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 gap-2">
                <div v-for="shortcut in shortcuts" :key="shortcut.url" class="mb-2"
                    @touchstart="touchStart($event, shortcut.title)" @touchend="touchEnd($event)"
                    @contextmenu.prevent="showContextMenu($event, shortcut.title)">
                    <Shortcut :url="shortcut.url" :title="shortcut.title" />
                </div>
            </div>
            <form @submit.prevent="addShortcut" class="flex flex-col items-center mt-8">
                <div>
                    <IconAddFolder
                        class="transition-transform transform cursor-pointer h-12 w-12 mt-4 hover:scale-105"
                        id="button-add-folder" @click="isButtonClicked = !isButtonClicked"
                        v-show="!isButtonClicked" />
                </div>
                <div id="icon-add-folder" v-show="isButtonClicked">
                    <div class="flex flex-col items-center">
                        <input v-model="newShortcutUrl" placeholder="URL"
                            class="flex w-48 h-8 px-4 mr-2 mb-2 text-sm text-white border-2 rounded-md placeholder-light-gray bg-dark-gray border-dark-gray focus:outline-none focus:border-blue"
                            required />
                        <input v-model="newShortcutTitle" placeholder="Title"
                            class="flex w-48 h-8 px-4 mr-2 text-sm text-white border-2 rounded-md placeholder-light-gray bg-dark-gray border-dark-gray focus:outline-none focus:border-blue"
                            required />
                        <button class="pt-2 transition-transform transform hover:scale-110" type="submit">
                            <IconSubmenuAddFolder />
                        </button>
                    </div>
                </div>
            </form>
            <div v-show="contextMenu.isVisible" class="context-menu" ref="subMenu"
                :style="{ left: contextMenu.x + 'px', top: contextMenu.y + 'px' }">
                <div class="flex flex-col w-48 text-left">
                    <button class="flex text-left text-sm py-1 rounded-sm hover:bg-[#D9D9D9] hover:bg-opacity-25"
                        @click="deleteShortcut(contextMenu.shortcutTitle)">
                        <div class="px-2">
                            <IconSubmenuDeleteFolder class="w-5 h-5" :color="'#838383'" />
                        </div>
                        <div>
                            Supprimer le raccourci
                        </div>
                    </button>
                    <button class="flex text-left text-sm py-1 rounded-sm hover:bg-[#D9D9D9] hover:bg-opacity-[12%]"
                        @click="isButtonClicked = true">
                        <div class="px-2">
                            <IconSubmenuAddFolder class="w-5 h-5" />
                        </div>
                        <div>
                            Nouveau raccourci
                        </div>
                    </button>
                    <button class="flex text-left text-sm py-1 rounded-sm hover:bg-[#D9D9D9] hover:bg-opacity-[12%]"
                        @click="contextMenu.isVisible = false">
                        <div class="px-2">
                            <IconSubmenuOut class="w-5 h-5" />
                        </div>
                        <div>
                            Retours
                        </div>
                    </button>
                </div>
            </div>
        </div>
    </div>
</template>

<script>
import axios from 'axios';
const BASE_URL = import.meta.env.VITE_BASE_URL

export default {
    data() {
        return {
            shortcuts: [],
            newShortcutUrl: '',
            newShortcutTitle: '',
            isButtonClicked: false,
            contextMenu: {
                isVisible: false,
                shortcutTitle: null,
                x: 0,
                y: 0
            },
        };
    },
    methods: {
        touchStart(event, shortcutTitle) {
            // Si le toucher est maintenu pendant 700ms
            // On affiche le menu contextuel
            this.touchTimeout = setTimeout(() => this.showContextMenu(event, shortcutTitle), 700)
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

        showContextMenu(event, shortcutTitle) {
            event.preventDefault()
            // Enregistrer le nom du dossier sélectionné
            this.contextMenu.shortcutTitle = shortcutTitle

            // Vérifier si l'option "Supprimer" a été sélectionnée
            if (event.target.textContent === "Supprimer") {
                this.deleteShortcut(shortcutTitle)
            } else {
                // clientX : Coordonnée X du pointeur de la souris
                this.contextMenu.x = event.clientX
                // clientY : Coordonnée Y du pointeur de la souris
                this.contextMenu.y = event.clientY
                // Afficher le menu contextuel
                this.contextMenu.isVisible = true
            }
        },

        async fetchShortcuts() {
            try {
                const token = this.getJwtToken();

                const headers = {
                    Authorization: `Bearer ${token}`
                };

                const response = await axios.get(`${BASE_URL}/api/Shortcut/user`, { headers });

                if (response.status !== 200) {
                    console.error('Erreur lors de la récupération des raccourcis:', response);
                    return;
                }

                this.shortcuts = response.data.map(shortcut => {
                    return {
                        ...shortcut,
                        title: shortcut.name
                    };
                });
            } catch (error) {
                console.error('Erreur lors de la récupération des raccourcis:', error);
            }
        },
        async addShortcut() {
            try {
                const token = this.getJwtToken()

                const headers = {
                    Authorization: `Bearer ${token}`
                };

                console.log(this.newShortcutUrl, this.newShortcutTitle);

                const response = await axios.post(`${BASE_URL}/api/Shortcut`, {
                    name: this.newShortcutTitle,
                    url: this.newShortcutUrl
                }, { headers });

                if (response.status !== 201) {
                    console.error('Erreur lors de l\'ajout du raccourci:', response);
                    return;
                }

                this.shortcuts.push(response.data);
                this.newShortcutUrl = '';
                this.newShortcutTitle = '';
                this.isButtonClicked = false;
            } catch (error) {
                console.error('Erreur lors de l\'ajout du raccourci:', error);
            }
            this.fetchShortcuts();
        },

        async deleteShortcut(shortcutTitle) {
            try {
                const token = this.getJwtToken()

                const headers = {
                    Authorization: `Bearer ${token}`
                };
                console.log(shortcutTitle);
                const response = await axios.delete(`${BASE_URL}/api/Shortcut/user/${shortcutTitle}`, { headers });

                if (response.status !== 204) {
                    console.error('Erreur lors de la suppression du raccourci:', response);
                    return;
                }
                this.fetchShortcuts();
                this.contextMenu.isVisible = false
            } catch (error) {
                console.error('Erreur lors de la suppression du raccourci:', error);
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
    mounted() {
        this.fetchShortcuts()
        document.addEventListener("click", this.handleClickOutside)
    },
    setup() {
        definePageMeta({
            middleware: ['auth'],
        });
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
