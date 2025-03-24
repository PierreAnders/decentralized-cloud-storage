export default defineNuxtRouteMiddleware(() => {
    if (process.client) { // Pour éxécuter coté client
        const jwt = localStorage.getItem('access_token');
        if (!jwt) {
            console.error("Le jeton JWT n'est pas disponible.");
            return navigateTo('/');
        }
    }
});
