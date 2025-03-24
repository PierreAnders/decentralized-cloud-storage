<template>
    <svg width="25" height="25" viewBox="0 0 18 18" fill="none" xmlns="http://www.w3.org/2000/svg">
        <circle cx="9" cy="9" r="8.5" :stroke="color" />
        <rect x="8" y="4" width="2" height="10" :fill="color" />
        <rect x="4" y="10" width="2" height="10" transform="rotate(-90 4 10)" :fill="color" />
    </svg>
</template>

<script>
export default {
    props: {
        color: String,
    }
}
</script>