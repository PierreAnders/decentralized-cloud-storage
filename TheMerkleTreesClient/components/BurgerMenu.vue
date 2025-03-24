<template>
    <div>
        <button @click="toggleMenu" class="block">
            <IconMenu class="transition-transform transform hover:scale-110" />
        </button>
        <div v-if="isOpen"
            class="background absolute top-0 left-0 z-10 w-1/2 h-full p-4 transition-transform duration-300 ease-in-out bg-dark-gray/90 sm:w-1/2 md:w-1/3 lg:w-1/4">
            <ul class="text-slate-400 mt-14">
                <li>
                    <nuxt-link to="/home"
                        class="flex items-center px-4 transition duration-300 rounded hover:bg-black hover:text-white">
                        <IconHome :color="'#838383'" class="mr-2" />
                        <span class="block py-2 font-semibold tracking-wide text-light-gray">Home</span>
                    </nuxt-link>
                </li>
                <li>
                    <nuxt-link to="/documents"
                        class="flex items-center px-4 transition duration-300 rounded hover:bg-black hover:text-white">
                        <IconDocument :color="'#838383'" class="mr-2" />
                        <span class="block py-2 font-semibold tracking-wide text-light-gray">Documents</span>
                    </nuxt-link>
                </li>
                <li>
                    <nuxt-link to="/chat"
                        class="flex items-center px-4 transition duration-300 rounded hover:bg-black hover:text-white">
                        <IconChat :color="'#838383'" class="mr-2" />
                        <span class="block py-2 font-semibold tracking-wide text-light-gray">Chat</span>
                    </nuxt-link>
                </li>
                <li>
                    <nuxt-link to="/code"
                        class="flex items-center px-4 transition duration-300 rounded hover:bg-black hover:text-white">
                        <IconCode :color="'#838383'" class="mr-2" />
                        <span class="block py-2 font-semibold tracking-wide text-light-gray">Code</span>
                    </nuxt-link>
                </li>
                <li>
                    <nuxt-link to="/note"
                        class="flex items-center px-4 transition duration-300 rounded hover:bg-black hover:text-white">
                        <IconNote :color="'#838383'" class="mr-2" />
                        <span class="block py-2 font-semibold tracking-wide text-light-gray">Notes</span>
                    </nuxt-link>
                </li>
                <!-- <li>
                    <nuxt-link to="/finance-menu"
                        class="flex items-center px-4 transition duration-300 rounded hover:bg-black hover:text-white">
                        <IconFinance :color="'#838383'" class="mr-2" />
                        <span class="block py-2 font-semibold tracking-wide text-light-gray">Finance</span>
                    </nuxt-link>
                </li>
                <li>
                    <nuxt-link to="/health"
                        class="flex items-center px-4 transition duration-300 rounded hover:bg-black hover:text-white">
                        <IconHealth :color="'#838383'" class="mr-2" />
                        <span class="block py-2 font-semibold tracking-wide text-light-gray">Santé</span>
                    </nuxt-link>
                </li> -->
                <li>
                    <nuxt-link to="/profile"
                        class="flex items-center px-4 mb-12 transition duration-300 rounded hover:bg-black hover:text-white">
                        <IconProfile :color="'#838383'" class="mr-2" />
                        <span class="block py-2 font-semibold tracking-wide text-light-gray">Profile</span>
                    </nuxt-link>
                </li>
                <li>
                    <nuxt-link to="/register"
                        class="flex items-center px-4 transition duration-300 rounded hover:bg-black hover:text-white">
                        <IconConnection :color="'#838383'" class="mr-2" />
                        <span class="block py-2 font-semibold tracking-wide text-light-gray">Inscription</span>
                    </nuxt-link>
                </li>
                <li>
                    <button @click="redirection"
                        class="flex items-center w-full px-4 transition duration-300 rounded hover:bg-black hover:text-white">
                        <IconRegister :color="'#838383'" class="mr-2" />
                        <span class="block py-2 font-semibold tracking-wide text-light-gray">{{ isConnected ? 'Déconnexion'
                            : 'Connexion' }}</span>
                    </button>
                </li>
            </ul>
        </div>
    </div>
</template>
  
<script>
import IconMenu from '@/components/IconMenu.vue'
import IconDashboard from '@/components/IconDashboard.vue'
import IconDocument from '@/components/IconDocument.vue'
import IconChat from '@/components/IconChat.vue'
import IconFinance from '@/components/IconFinance.vue'
import IconHealth from '@/components/IconHealth'
import IconProfile from '@/components/IconProfile.vue'
import IconConnection from '@/components/IconConnection.vue'
import IconRegister from '@/components/IconRegister.vue'

export default {
    components: {
        IconMenu,
        IconDashboard,
        IconDocument,
        IconChat,
        IconFinance,
        IconHealth,
        IconProfile,
    },
    data() {
        return {
            isOpen: false,
            isConnected: false,
            jwtToken: null,
        };
    },
    methods: {
        connectionState() {
            if (process.client) {
                this.jwtToken = localStorage.getItem('access_token');
                if (this.jwtToken) {
                    this.isConnected = true;
                } else {
                    this.isConnected = false;
                }
            }
        },
        redirection() {
            if (this.isConnected) {
                this.logout()
            } else {
                this.$router.push('/');
            }
        },
        toggleMenu() {
            this.isOpen = !this.isOpen;
        },
        closeMenuOnClickOutside(event) {
            const menuElement = this.$el.querySelector(".background");
            const buttonElement = this.$el.querySelector("button");

            if (menuElement && buttonElement && !menuElement.contains(event.target) && !buttonElement.contains(event.target)) {
                this.isOpen = false;
            }
        },
        logout() {
            localStorage.removeItem("access_token");
            this.$router.push("/");
        },
    },
    mounted() {
        // Écoute les clics au niveau de la page pour détecter les clics en dehors du menu
        window.addEventListener("click", this.closeMenuOnClickOutside);
        this.connectionState();
    },
    beforeDestroy() {
        // Supprimez l'écouteur de clics lorsque le composant est détruit
        window.removeEventListener("click", this.closeMenuOnClickOutside);
    },
};
</script>
  