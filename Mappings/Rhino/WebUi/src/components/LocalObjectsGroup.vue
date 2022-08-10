<template>
  <v-card style="transition: all .2s ease" :class="`rounded-lg mb-3 ${hover ? 'elevation-4' : ''} overflow-hidden`"
    @mouseenter="hover=true"
    @mouseleave="hover=false"
    @dblclick.stop="handleDoubleClick()"
    >
    <v-toolbar dense flat>
      <v-app-bar-nav-icon @click="expand = !expand">
        <v-icon small>mdi-plus</v-icon>
      </v-app-bar-nav-icon>
      <b>{{group.name}}s</b>
      <span class="ml-2">{{group.objs.length}}</span>
    </v-toolbar>
    <v-slide-y-transition>
      <div v-show="expand" class="pl-4 px-2 mb-4">
        <div v-for="obj in group.objs" :key="obj.objectId" @mouseenter="hoverObject(obj.objectId)" @mouseleave="hoverAll()" @dblclick.stop="handleDoubleClick(obj.objectId)">
          <div class="d-flex caption">
            <div class="mr-2 primary--text">{{group.name}}</div>
            <div v-for="key in getRelevantKeys(obj)" :key="key" class="mr-1">
              {{key}}: <b>{{obj[key]}}</b>
            </div>
          </div>
        </div>
      </div>
    </v-slide-y-transition>
  </v-card>
</template>
<script>
export default {
  props:{
    group:{
      type: Object,
      default: () => null
    }
  },
  watch:{
    hover(val) {
      console.log(this.group.objs)
      if(val) {
        window.chrome.webview.postMessage({
          action: 'set-hover',
          objectIds: this.group.objs.map( o => o.objectId)
        })
      } else {
        window.chrome.webview.postMessage({
          action: 'set-hover',
          objectIds: []
        })
      }
    }
  },
  data: () => ({
    expand: false,
    hover: false
  }),
  methods:{
    getRelevantKeys(obj){
      let hiddenKeys = ['schemaName', 'objectId']
      let allKeys = Object.keys(obj)
      return allKeys.filter( key => hiddenKeys.indexOf(key) === -1)
    },
    hoverAll(){
      window.chrome.webview.postMessage({
          action: 'set-hover',
          objectIds: this.group.objs.map( o => o.objectId)
      })
    },
    hoverObject(id) {
      window.chrome.webview.postMessage({
        action: 'set-hover',
        objectIds: [id]
      })
    },
    unHover(){
      window.chrome.webview.postMessage({
        action: 'set-hover',
        objectIds: []
      })
    },
    handleDoubleClick( id ){
      window.chrome.webview.postMessage({
        action: 'set-select',
        objectIds: id ? [id] : this.group.objs.map( o => o.objectId)
      })
    }
  }
}
</script>