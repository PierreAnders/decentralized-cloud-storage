<template>
    <div class="min-h-screen px-8 pt-8">
        <BurgerMenu />
        <div class="flex items-center justify-center pt-8">
            <h1 class="pr-3 tracking-wider text-light-gray">RECETTES</h1>
            <IconExpenses class="transform scale-x-[-1]" :color="'#334155'" />
        </div>
        <form @submit.prevent="submitIncomeInfo">
            <div class="flex flex-col items-center justify-center mt-12">
                <div class="flex flex-col pb-4 w-72">
                    <label class="sr-only" for="title">Titre</label>
                    <input
                        class="h-8 px-4 text-sm text-white border-2 rounded-md bg-dark-gray placeholder-light-gray w-72 border-dark-gray focus:outline-none focus:border-blue"
                        type="text" id="title" v-model="incomeInfo.title" placeholder="Titre :">
                </div>
                <div class="flex flex-col pb-4 w-72">
                    <label class="sr-only" for="description">Description</label>
                    <textarea
                        class="h-20 px-4 pt-1 text-sm text-white border-2 rounded-md bg-dark-gray placeholder-light-gray w-72 border-dark-gray focus:outline-none focus:border-blue"
                        type="text" id="description" v-model="incomeInfo.description"
                        placeholder="Description :"></textarea>
                </div>
                <div class="flex flex-col pb-4 w-72">
                    <div class="flex">
                        <label class="sr-only" for="price">Prix</label>
                        <input
                            class="h-8 px-4 mr-2 text-sm text-white border-2 rounded-md bg-dark-gray placeholder-light-gray w-72 border-dark-gray focus:outline-none focus:border-blue"
                            type="number" step="0.01" id="price" v-model="incomeInfo.price" placeholder="Prix :">
                        <button type="submit">
                            <IconEnter class="transition-transform transform hover:scale-110" />
                        </button>
                    </div>
                </div>
                <div v-if="successSubmit" class="relative pb-4 text-sm text-white w-72">
                    La recette a bien été ajoutée.
                </div>
                <div class="w-full mb-24 md:w-3/4 lg:w-2/3">
                    <div class="flex my-6">
                        <div class="flex mr-2 font-semibold text-light-gray">TOTAL :</div>
                        <div class="text-white"> {{ total.toFixed(2) }} €</div>
                    </div>

                    <div v-for="income in incomes" :key="income.id">
                        <div class="mt-5">
                            <div class="flex mt-1">
                                <div class="mr-2 text-light-gray">Titre:</div>
                                <div class="text-white">{{ income.title }}</div>
                            </div>
                            <div class="flex mt-1">
                                <div class="mr-2 text-light-gray">Prix:</div>
                                <div class="text-white">{{ income.price }} €</div>
                            </div>
                            <div class="flex mt-1">
                                <div class="flex mr-2 text-sm text-light-gray">Description:</div>
                                <div class="text-sm text-white">{{ income.description }}</div>
                            </div>
                            <button class="mt-2" @click="deleteIncome(income.id)">
                                <IconSubmenuDeleteFolder class="w-5 h-5 transition-transform transform hover:scale-110"
                                    color="#553348" />
                            </button>
                        </div>
                    </div>
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
            incomeInfo: {
                title: "",
                description: "",
                price: "",
            },
            incomes: {},
            total: 0,
            successSubmit: false,
        };
    },

    methods: {
        async getAllIncomes() {
            try {
                const token = localStorage.getItem("access_token");

                if (!token) {
                    console.error("Jeton JWT non trouvé.");
                    return;
                }

                const headers = {
                    Authorization: `Bearer ${token}`
                };

                const response = await axios.get("http://localhost:5000/incomes", { headers });
                console.log('date', response.data)
                if (response.status === 200) {
                    this.incomes = response.data;
                    this.incomes.sort((a, b) => b.price - a.price);
                    this.total = this.calculateTotal();
                } else {
                    console.error("Échec de la récupération des recettes.");
                }
            } catch (error) {
                console.error("Erreur lors de la récupération des recettes :", error);
            }
        },

        calculateTotal() {
            return this.incomes.reduce((total, income) => total + parseFloat(income.price), 0);
        },

        async submitIncomeInfo() {
            try {
                const token = localStorage.getItem("access_token");

                if (!token) {
                    console.error("Jeton JWT non trouvé.");
                    return;
                }

                const headers = {
                    Authorization: `Bearer ${token}`
                };

                const response = await axios.post(`${BASE_URL}/incomes`, this.incomeInfo, { headers });

                if (response.status === 201) {
                    this.successSubmit = true;
                    console.log("Enregistrement d'une nouvelle recette'.")
                    this.getAllIncomes();
                    this.resetIncomeInfo();
                } else {
                    console.error("Échec de l'enregistrement d'une recette'.");
                }
            } catch (error) {
                console.error("Erreur lors de la soumission d'une recette':", error);
            }
        },

        resetIncomeInfo() {
            this.incomeInfo.title = "";
            this.incomeInfo.description = "";
            this.incomeInfo.price = "";
        },

        async deleteIncome(incomeId) {
            try {
                const token = localStorage.getItem("access_token");

                if (!token) {
                    console.error("Jeton JWT non trouvé.");
                    return;
                }

                const headers = {
                    Authorization: `Bearer ${token}`,
                };

                const response = await axios.delete(`http://localhost:5000/incomes/${incomeId}`, { headers });

                if (response.status === 200) {
                    console.log("Recette supprimée avec succès.");
                    this.getAllIncomes();
                } else {
                    console.error("Échec de la suppression de la recette.");
                }
            } catch (error) {
                console.error("Erreur lors de la suppression de la recette :", error);
            }
        },
    },

    setup() {
        definePageMeta({
            middleware: ['auth'],
        });
    },

    mounted() {
        this.getAllIncomes();
    },
};
</script>
    
<style scoped>
/* Cacher les flèches dans les inputs de type number */
/* Chrome, Safari, Edge, Opera */
input::-webkit-outer-spin-button,
input::-webkit-inner-spin-button {
  -webkit-appearance: none;
  margin: 0;
}
/* Firefox */
input[type=number] {
  -moz-appearance: textfield;
  appearance: textfield;
}
</style>