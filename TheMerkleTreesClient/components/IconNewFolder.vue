<template>
  <svg width="40" height="40" viewBox="0 0 26 21" fill="none" xmlns="http://www.w3.org/2000/svg">
      <path
          d="M1 1.92308V18.4066C1 19.4615 1.87912 19.4615 2.31868 19.4615H23.8132C24.7626 19.4615 25 18.7582 25 18.4066V4.69231C25 3.42637 24.2088 2.93407 23.8132 2.84615H9.7033C9.13161 2.84615 8.7083 1 8.12088 1H2.05495C1.31648 1 1.04396 1.61538 1 1.92308Z"
          :stroke="color" stroke-width="1" />
      <path
          d="M2.5 6C1.33956 6 1 7.69231 1 8V18.2747C1 19.3297 1.65934 19.4616 2.31868 19.4616H23.6813C24.6968 19.4616 25 18.9341 25 18.2747V8C25 6.94506 24.297 6 23.6813 6H2.5Z"
          :stroke="color" stroke-width="1" />
  </svg>
</template>
  
<script>
export default {
    props: {
        color: String,
    }
}
</script>