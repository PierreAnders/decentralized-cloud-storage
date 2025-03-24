<template>
    <div class="min-h-screen px-8 pt-8">
        <BurgerMenu />
        <div class="flex items-center justify-center pt-8">
            <h1 class="pr-3 tracking-wider text-light-gray">FINANCE</h1>
            <IconFinance :color="'#334155'" />
        </div>
        <div class="flex flex-col items-center justify-center mt-12">
            <div
                class="flex items-center justify-between h-8 px-4 mb-4 text-sm text-white rounded-md bg-dark-gray placeholder-light-gray w-72">
                <div class="flex space-x-3">
                    <IconBalance color="#838383" />
                    <div class="text-light-gray">Balances</div>
                </div>
                <div>{{ (totalIncomes - totalExpenses).toFixed(2) }} €</div>
            </div>
            <div class="flex pb-2 space-x-2 w-72">
                <nuxt-link
                    class="flex items-center justify-between h-8 px-4 mb-2 text-sm text-white transition duration-300 rounded-md bg-dark-gray hover:bg-opacity-75 w-72"
                    to="/finance/expenses">
                    <div class="flex space-x-3">
                        <IconExpenses color="#838383" />
                        <div class="text-light-gray">Dépenses</div>
                    </div>
                    <div>{{ totalExpenses.toFixed(2) }} €</div>
                </nuxt-link>
                <nuxt-link to="/finance/expenses">
                    <IconEnter class="transition-transform transform hover:scale-110" />
                </nuxt-link>
            </div>
            <div class="flex pb-2 space-x-2 w-72">
                <nuxt-link
                    class="flex items-center justify-between h-8 px-4 mb-2 text-sm text-white transition duration-300 rounded-md bg-dark-gray hover:bg-opacity-75 w-72"
                    to="/finance/incomes">
                    <div class="flex space-x-3">
                        <IconExpenses class="transform scale-x-[-1]" color="#838383" />
                        <div class="text-light-gray">Recettes</div>
                    </div>
                    <div>{{ totalIncomes.toFixed(2) }} €</div>
                </nuxt-link>
                <nuxt-link to="/finance/incomes">
                    <IconEnter class="transition-transform transform hover:scale-110" />
                </nuxt-link>
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
            totalIncomes: 0,
            totalExpenses: 0,
            incomes: {},
            expenses: {},
            userInfo: {
                lastname: "",
                firstname: "",
                birth_date: "",
                email: "",
            },
        };
    },

    methods: {
        async getAllIncomes() {
            try {
                const token = this.getJwtToken()

                const headers = {
                    Authorization: `Bearer ${token}`
                };

                const response = await axios.get(`${BASE_URL}/incomes`, { headers })

                if (response.status === 200) {
                    this.incomes = response.data;
                    this.incomes.sort((a, b) => b.price - a.price)
                    this.totalIncomes = this.calculateTotal(this.incomes)
                } else {
                    console.error("Échec de la récupération des recettes.")
                }
            } catch (error) {
                console.error("Erreur lors de la récupération des recettes :", error)
            }
        },

        async getAllExpenses() {
            try {
                const token = this.getJwtToken()

                const headers = {
                    Authorization: `Bearer ${token}`
                };

                const response = await axios.get(`${BASE_URL}/expenses`, { headers })

                if (response.status === 200) {
                    this.expenses = response.data;
                    this.expenses.sort((a, b) => b.price - a.price)
                    this.totalExpenses = this.calculateTotal(this.expenses)
                } else {
                    console.error("Échec de la récupération des dépenses.")
                }
            } catch (error) {
                console.error("Erreur lors de la récupération des dépenses :", error)
            }
        },

        calculateTotal(elements) {
            return elements.reduce((total, element) => total + parseFloat(element.price), 0)
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
        this.getAllIncomes();
        this.getAllExpenses();
    },
};
</script>