export const jwt = getJwtToken(() =>  {
    if (process.client) {
    const jwtToken = localStorage.getItem('access_token');

    if (!jwtToken) {
        console.error('Le jeton JWT n\'est pas disponible.');
        return navigateTo('/');
    }
    return jwtToken;
    }
});