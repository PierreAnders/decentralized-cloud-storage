<template>
    <svg width="25" height="25" viewBox="0 0 17 13" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M11 1L7 12" :stroke="color" stroke-linecap="round" />
        <path d="M4.5 3L1 6.5" :stroke="color" stroke-linecap="round" />
        <path d="M4.5 9.5L1 6.5" :stroke="color" stroke-linecap="round" />
        <path d="M13 3L16.5 6.5" :stroke="color" stroke-linecap="round" />
        <path d="M13 9.5L16.5 6.5" :stroke="color" stroke-linecap="round" />
    </svg>
</template>

<script>
export default {
    props: {
        color: String,
    }
}
</script>