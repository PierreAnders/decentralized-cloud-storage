<template>
    <div class="min-h-screen px-8 pt-8">
        <!-- <ThreeGalaxy id="project-info"/> -->
        <BurgerMenu />
        <div class="flex items-center justify-center pt-8">
            <h1 class="pr-3 tracking-wider text-light-gray">PROFILE</h1>
            <IconProfile :color="'#334155'" />
        </div>
        <form @submit.prevent="updateUserInfo">
            <div class="flex flex-col items-center justify-center mt-12">
                <div class="relative pb-4 w-72">
                    <label class="text-light-gray absolute text-sm px-2 top-1.5 left-2" for="nom">Nom:</label>
                    <input
                    class="h-8 px-4 text-sm text-right text-white border-2 rounded-md bg-dark-gray placeholder-light-gray w-72 border-dark-gray focus:outline-none focus:border-blue"
                    type="text" id="nom" v-model="userInfo.firstname" placeholder=". . . . . . .">
                </div>
                <div class="relative pb-4 w-72">
                    <label class="text-light-gray absolute text-sm px-2 top-1.5 left-2" for="nom">Prénom:</label>
                    <input
                    class="h-8 px-4 text-sm text-right text-white border-2 rounded-md bg-dark-gray placeholder-light-gray w-72 border-dark-gray focus:outline-none focus:border-blue"
                    type="text" id="nom" v-model="userInfo.lastname" placeholder=". . . . . . .">
                </div>
                <div class="relative pb-4 w-72">
                    <label class="text-light-gray absolute text-sm px-2 top-1.5 left-2" for="email">Email:</label>
                    <input
                    class="h-8 px-4 text-sm text-right text-white border-2 rounded-md bg-dark-gray placeholder-light-gray w-72 border-dark-gray focus:outline-none focus:border-blue"
                    type="email" id="email" v-model="userInfo.email" required placeholder=". . . . . . .">
                </div>
                <div class="relative flex pb-4 w-72">
                    <label class="text-light-gray absolute text-sm px-2 top-1.5 left-2" for="nom">Né le :</label>
                    <input
                    class="h-8 px-4 mr-2 text-sm text-right text-white border-2 rounded-md bg-dark-gray placeholder-light-gray w-72 border-dark-gray focus:outline-none focus:border-blue"
                    type="date" id="nom" v-model="userInfo.birth_date" placeholder=". . . . . . .">
                    <button type="submit">
                        <IconEnter class="transition-transform transform hover:scale-110" />
                    </button>
                </div>
            </div>
        </form>
    </div>
</template>

<script>
import axios from 'axios'
const BASE_URL = import.meta.env.VITE_BASE_URL

export default {
    data() {
        return {
            userInfo: {
                firstname: '',
                lastname: '',
                birth_date: '',
                email: '',
            }
        }
    },

    methods: {
        async updateUserInfo() {
            try {
                const token = this.getJwtToken()

                const headers = {
                    Authorization: `Bearer ${token}`
                };

                await axios.put(`${BASE_URL}/users/update`, {
                    firstname: this.userInfo.firstname,
                    lastname: this.userInfo.lastname,
                    birth_date: this.userInfo.birth_date,
                    email: this.userInfo.email,
                },
                    { headers });

            } catch (error) {
                console.error('Erreur d\'inscription :', error);
            }
        },

        async getUserInfo() {
            try {
                const token = this.getJwtToken()

                const headers = {
                    Authorization: `Bearer ${token}`
                };

                const response = await axios.get(`${BASE_URL}/users/info`, { headers });

                if (response.status === 200) {
                    this.userInfo = response.data;
                } else {
                    console.error("Échec de la récupération des données de l'utilisateur.");
                }
            } catch (error) {
                console.error("Erreur lors de la récupération des données de l'utilisateur :", error);
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

    created() {
        this.getUserInfo();
    }
};
</script>
<style>

#project-info {
position: absolute;
  z-index: 0;
  top: 194px;
}
</style>