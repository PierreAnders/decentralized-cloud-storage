<template>
    <svg width="25" height="25" viewBox="0 0 19 23" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M1 4H3" :stroke="color" stroke-width="0.8" stroke-linecap="round" />
        <path d="M5 6L15 6" :stroke="color" stroke-linecap="round" />
        <path d="M5 9L15 9" :stroke="color" stroke-linecap="round" />
        <path d="M1 7H3" :stroke="color" stroke-width="0.8" stroke-linecap="round" />
        <path d="M1 10H3" :stroke="color" stroke-width="0.8" stroke-linecap="round" />
        <path d="M1 13H3" :stroke="color" stroke-width="0.8" stroke-linecap="round" />
        <path d="M1 16H3" :stroke="color" stroke-width="0.8" stroke-linecap="round" />
        <path d="M1 19H3" :stroke="color" stroke-width="0.8" stroke-linecap="round" />
        <path d="M5 12L15 12" :stroke="color" stroke-linecap="round" />
        <path d="M5 15L15 15" :stroke="color" stroke-linecap="round" />
        <path d="M10 1H3.5C2.5 1 2 1.5 2 2.5C2 3.7 2 15.5 2 21C2 22 2.33772 22.5 3.5 22.5C5.1 22.5 8.66667 22.5 10 22.5"
            :stroke="color" />
        <path
            d="M10 1H16.5C17.5 1 18 1.5 18 2.5C18 3.7 18 15.5 18 21C18 22 17.6623 22.5 16.5 22.5C14.9 22.5 11.3333 22.5 10 22.5"
            :stroke="color" />
    </svg>
</template>

<script>
export default {
    props: {
        color: String,
    }
}
</script>