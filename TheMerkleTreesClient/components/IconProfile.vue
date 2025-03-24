<template>
    <svg width="25" height="25" viewBox="0 0 17 18" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M16.1305 17.4001C16.1305 17.4001 16.1305 9.07837 8.56528 9.07837" :stroke="color" />
        <path d="M1 17.4001C1 17.4001 1 9.07837 8.56522 9.07837" :stroke="color" />
        <circle cx="8.56513" cy="4.53913" r="4.03913" :stroke="color" />
        <path d="M1 17.4C10.311 17.4 16.1304 17.4001 16.1304 17.4001" :stroke="color" stroke-linecap="round" />
    </svg>
</template>

<script>
export default {
    props: {
        color: String,
    }
}
</script>