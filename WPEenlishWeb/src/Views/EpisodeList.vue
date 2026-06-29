<template>
  <div class="page-container">
    <div class="page-header">
      <h2 class="page-title">片段管理</h2>
      <div class="page-actions">
        <el-button type="primary" v-if="isSorting" @click="cancelSort">取消排序</el-button>
        <el-button type="primary" @click="triggerSort">{{ isSorting ? "保存排序" : "排序" }}</el-button>
        <el-button type="primary" v-if="!isSorting" @click="add(router.currentRoute.value.query.albumId)">+ 添加片段</el-button>
      </div>
    </div>

    <div class="card">
      <el-table :data="state.tableData" style="width: 100%" stripe>
        <el-table-column #default="scope" label="序号" width="80" >{{ scope.$index + 1 }}</el-table-column>
        <el-table-column prop="title" label="标题" />
        <el-table-column prop="durationInSecond" label="时长(秒)" width="100" />
        <el-table-column prop="createTime" label="创建时间" width="180" />
        <el-table-column label="操作" width="140">
          <template #default="scope">
             <template v-if="isSorting">
              <el-button link type="primary" size="small" :disabled="scope.$index === 0" @click="moveUp(scope.$index)">上移</el-button>
              <el-button link type="danger" size="small" :disabled="scope.$index === state.tableData.length - 1" @click="moveDown(scope.$index)">下移</el-button>
            </template>
            <template v-else>
              <el-button link type="primary" size="small" @click="edit(scope.row)">编辑</el-button>
              <el-button link type="danger" size="small" @click="dele(scope.row.id)">删除</el-button>
            </template>
          </template>

        </el-table-column>
      </el-table>
    </div>
  </div>
</template>

<script setup>
import request from '@/utils/request';
import { ElMessage } from 'element-plus';
import { onMounted, reactive,ref } from 'vue'
import {useRouter} from 'vue-router'
import ConfirmBox from '@/utils/ConfirmBox';

const state = reactive({
	tableData:[]
})
const router = useRouter()
const isSorting = ref(false)

onMounted(async () => {
	fetchEpisodesByAlbumId()
})

const fetchEpisodesByAlbumId = async()=>{
	try{
		const response = await request.get(`/admin/episode/GetAllEpisodes?albumId=${router.currentRoute.value.query.albumId}`)
		if(response.data.data){
			state.tableData = response.data.data.filter(s=>!s.isDeleted)
		}else{
			state.tableData = []
		}
		
	}catch(error){
		ElMessage.error("获取集数列表失败,请稍后重试")
	}
}
const add = (albumId) => {
	router.push({path:"/episodeAdd",query:{albumId:albumId}})
}
const edit = async(row)=>{
	router.push({path:"/episodeUpdate",query:row})
}
const dele = async (id) => {
  const confirm = await ConfirmBox("确定删除吗")
  if(!confirm) return
	try{
		const response = await request.delete(`/admin/episode/Delete?id=${id}`)
		if(response.status === 200){
			ElMessage.success("删除成功")
			fetchEpisodesByAlbumId()
		}
	}catch(erroe){
		ElMessage.error("删除失败,请检查网络连接")
	}
}
const cancelSort = async()=>{
	isSorting.value = false
  fetchEpisodesByAlbumId()
}
const triggerSort = async()=>{
  if(!isSorting.value){
    isSorting.value = true
    return
  }

  try{
    const sortedIds = state.tableData.map(a => a.id)
    const response = await request.put("admin/episode/Reorder",{
      sortedAlbumsIds: sortedIds,
      albumId:router.currentRoute.value.query.albumId
    })

    if(response.status === 200){
      ElMessage.success("排序保存成功")
      fetchEpisodesByAlbumId()
      isSorting.value = false
    }
  }catch(error){
    ElMessage.error("排序保存失败")
  }
}
const moveUp = async(index)=>{
  if(index <= 0) return
  const list = state.tableData
  ;[list[index-1],list[index]] = [list[index],list[index-1]]
}
const moveDown = async(index)=>{
  const list = state.tableData
  if(index > list.length - 1) return
  ;[list[index],list[index+1]] = [list[index+1],list[index]]
}

</script>

<style scoped>
.page-container {
  max-width: 960px;
  margin: 0 auto;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 24px;
}

.page-title {
  font-size: 22px;
  font-weight: 600;
  color: var(--text-primary);
  letter-spacing: -0.3px;
}

.card {
  background: var(--bg-card);
  border-radius: var(--radius-lg);
  box-shadow: var(--shadow-card);
  border: 1px solid rgba(0, 0, 0, 0.04);
  padding: 24px;
}
</style>
