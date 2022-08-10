import Vue from 'vue'
import Vuex from 'vuex'

Vue.use(Vuex)

// window.clientUpdate = ( eventType,  ) {

// }

export default new Vuex.Store({
  state: {
    selectionInfo: {},
    existingObjects:[]
  },
  getters: {
  },
  mutations: {
    setSelection(state, {selectionInfo}) {
      state.selectionInfo = selectionInfo
    },
    setExistingObjects(state, { objects } ) {
      state.existingObjects = objects
    }
  },
  actions: {
  },
  modules: {
  }
})
