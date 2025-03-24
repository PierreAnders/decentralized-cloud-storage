<template>
    <svg width="26" height="23" viewBox="0 0 26 23" fill="none" xmlns="http://www.w3.org/2000/svg">
        <path d="M13 5.55939V21.4" :stroke="color" stroke-width="1.2" stroke-linecap="square"/>
        <path d="M10.8156 2.68161L11.534 2.89712L11.103 4.33386L10.3846 4.11835L10.8156 2.68161ZM4.38459 2.31835C3.98784 2.19932 3.7627 1.78121 3.88173 1.38446C4.00075 0.98772 4.41886 0.762582 4.81561 0.881606L4.38459 2.31835ZM10.3846 4.11835L4.38459 2.31835L4.81561 0.881606L10.8156 2.68161L10.3846 4.11835Z" :fill="color"/>
        <path d="M21.3999 6.39998L14.7999 4.59998" :stroke="color" stroke-width="1.2" stroke-linecap="round"/>
        <path d="M19 21.4H7" :stroke="color" stroke-width="1.2" stroke-linecap="round"/>
        <path d="M21.4 6.40002L25 14.8C25 16 24.28 18.4 21.4 18.4C18.52 18.4 17.8 16 17.8 14.8" :stroke="color" stroke-width="1.2" stroke-linecap="round"/>
        <path d="M17.7999 14.8L21.3999 6.40002" :stroke="color" stroke-width="1.2" stroke-linecap="round"/>
        <path d="M25 14.8H17.8" :stroke="color" stroke-width="1.2" stroke-linecap="round"/>
        <path d="M4.60007 1.59998L8.20007 9.99998C8.20007 11.2 7.48007 13.6 4.60007 13.6C1.72007 13.6 1.00007 11.2 1.00007 9.99998" :stroke="color" stroke-width="1.2" stroke-linecap="round"/>
        <path d="M1 9.99998L4.6 1.59998" :stroke="color" stroke-width="1.2" stroke-linecap="round"/>
        <path d="M8.20007 10H1.00007" :stroke="color" stroke-width="1.2" stroke-linecap="round"/>
        <circle cx="2.4" cy="2.4" r="1.65" transform="matrix(-1 0 0 1 15.3999 1)" :stroke="color" stroke-width="1.2"/>
    </svg>
</template>

<script>
export default {
    props: {
        color: String,
    }
}
</script>