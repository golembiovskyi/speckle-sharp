<template>
  <v-container>
    <v-row dense v-if="existingObjects.length !== 0">
      <v-col cols="12" class="caption grey--text">
        Existing schema assignments:
      </v-col>
      <v-col cols="12">      
        <local-objects-group v-for="group in groupedObjects" :key="group.name" :group="group"/>
      </v-col>
    </v-row>
    <v-row v-else>
      <v-col cols="12" class="caption grey--text">
        No current objects with schemas in the current document.
      </v-col>
    </v-row>
  </v-container>
</template>

<script>
import { mapState } from 'vuex'
import LocalObjectsGroup from '@/components/LocalObjectsGroup.vue'

export default {
  name: 'HelloWorld',
  components: { LocalObjectsGroup },
  computed:{
    ...mapState(['existingObjects']),
    groupedObjects() {
      let groups = {}
      for(let obj of this.existingObjects){
        let groupName = obj.schemaName
        if(!groups[groupName]) groups[groupName] = []
        groups[groupName].push(obj)
      }
      let groupsArray = []
      for(let key in groups){
        groupsArray.push({name:key, objs: groups[key]})
      }
      return groupsArray
    }
  },
  data: () => ({
    items:[
      'Floor', 'Wall', 'Grid Line', 'Column', 'Beam', 'Pipe', 'Duct'
    ]
  })
}
</script>
