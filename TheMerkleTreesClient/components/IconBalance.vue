<template>
    <svg width="26" height="23" viewBox="0 0 22 18" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M11 3.79956V17.0001" :stroke="color" stroke-linecap="square" />
        <path
            d="M4 1.5C3.72386 1.5 3.5 1.72386 3.5 2C3.5 2.27614 3.72386 2.5 4 2.5V1.5ZM9 2.5H9.5V1.5H9V2.5ZM4 2.5H9V1.5H4V2.5Z"
            :fill="color" />
        <path d="M6 17H16" :stroke="color" stroke-linecap="round" />
        <path d="M4 2L1 9C1 10 1.6 12 4 12C6.4 12 7 10 7 9" :stroke="color" stroke-linecap="round" />
        <path d="M7 9L4 2" :stroke="color" stroke-linecap="round" />
        <path d="M1 9H7" :stroke="color" stroke-linecap="round" />
        <path
            d="M18 1.5C18.2761 1.5 18.5 1.72386 18.5 2C18.5 2.27614 18.2761 2.5 18 2.5V1.5ZM13 2.5H12.5V1.5H13V2.5ZM18 2.5H13V1.5H18V2.5Z"
            :fill="color" />
        <path d="M18 2L21 9C21 10 20.4 12 18 12C15.6 12 15 10 15 9" :stroke="color" stroke-linecap="round" />
        <path d="M15 9L18 2" :stroke="color" stroke-linecap="round" />
        <path d="M21 9H15" :stroke="color" stroke-linecap="round" />
        <circle cx="11" cy="2" r="1.5" :stroke="color" />
    </svg>
</template>

<script>
export default {
    props: {
        color: String,
    }
}
</script>