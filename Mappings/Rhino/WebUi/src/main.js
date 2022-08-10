/* eslint-disable */
import Vue from 'vue'
import App from './App.vue'
import vuetify from './plugins/vuetify'
import store from './store'

Vue.config.productionTip = false

const Interop = new Vue()
window.Interop = Interop

new Vue({
  vuetify,
  store,
  render: h => h(App),
  mounted() {
    Interop.$on('object-selection', (selectionInfo) => {
      this.$store.commit({
        type:'setSelection',
        selectionInfo: JSON.parse(selectionInfo)
      })
    })
    
    Interop.$on('object-schemas', (objects) => {
      this.$store.commit({
        type:'setExistingObjects',
        objects: JSON.parse(objects)
      })
    })
  }
}).$mount('#app')
