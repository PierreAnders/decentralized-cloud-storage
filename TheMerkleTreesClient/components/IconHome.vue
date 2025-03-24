<template>
    <svg width="25" height="25" viewBox="0 0 21 17" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M1 8.5L10.5 1L20 8.5" :stroke="color" stroke-linecap="round" />
        <path d="M18 16V9.5M3 15.5V9.5" :stroke="color" stroke-linecap="round" />
        <path d="M9 16V10H12V16" :stroke="color" stroke-linecap="round" />
        <path d="M1 16H20" :stroke="color" stroke-linecap="round" />
    </svg>
</template>

<script>
export default {
    props: {
        color: String,
    }
}
</script>