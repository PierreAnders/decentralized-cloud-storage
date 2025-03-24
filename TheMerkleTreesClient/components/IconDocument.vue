<template>
    <svg width="25" height="25" viewBox="0 0 15 19" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path
            d="M2.16667 1H9.26389C9.75 1 9.91204 1.37569 9.94444 1.56354V4.38122C9.94444 5.05746 10.5926 5.22652 10.9167 5.22652H13.5417C14.0083 5.22652 14.2222 5.41436 14.2222 5.60221V17.1547C14.2222 17.8309 13.7037 18 13.4444 18H1.875C1.175 18 1 17.4365 1 17.1547V2.22099C1 1.2442 1.77778 1 2.16667 1Z"
            :stroke="color" stroke-linecap="round" />
        <path d="M9.94446 1.38641L13.8333 5.25005" :stroke="color" stroke-linecap="round" />
    </svg>
</template>

<script>
export default {
    props: {
        color: String,
    }
}
</script>