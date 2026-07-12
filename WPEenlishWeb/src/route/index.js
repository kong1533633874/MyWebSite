import { createRouter,createWebHistory } from "vue-router";

import CategoryList from '../Views/CategoryList.vue'
import CategoryAdd  from '../Views/CategoryAdd.vue'
import CategoryUpdate from '../Views/CategoryUpdate.vue'
import AlbumAdd from "@/Views/AlbumAdd.vue";
import AlbumList from "@/Views/AlbumList.vue";
import AlbumUpdate from "@/Views/AlbumUpdate.vue";
import EpisodeList from "@/Views/EpisodeList.vue";
import EpisodeAdd from "@/Views/EpisodeAdd.vue";
import EpisodeUpdate from "@/Views/EpisodeUpdate.vue";
import UserAdd from "@/Views/UserAdd.vue";
import UserList from "@/Views/UserList.vue";
import UserUpdate from "@/Views/UserUpdate.vue";
import Login from '../Views/Login.vue'
import Category from "@/SightseerViews/Category.vue";
import Album from "@/SightseerViews/Album.vue";
import Episode from "@/SightseerViews/Episode.vue";
import AudioPlayer from "@/SightseerViews/AudioPlayer.vue";
import BlogList from "@/SightseerViews/ResourceSites/BlogList.vue"
import Resources from "@/SightseerViews/ResourceSites/Resources.vue";
import PostDetail from "@/SightseerViews/ResourceSites/PostDetail.vue";
import InfoPage from "@/SightseerViews/InfoPage.vue";

const paths = [
    { path: "/", redirect: "/category" },
    {
        path:"/login",  
        component:Login
    },
    // ===== 以下为管理页面，需要登录 =====
    {
        path:"/categoryList",
        component:CategoryList,
        meta:{requiresAuth : true}
    },
    {
        path:"/categoryAdd",
        component:CategoryAdd,
        meta:{requiresAuth : true}
    },
    {        
        path:"/categoryUpdate",
        component:CategoryUpdate,
        meta:{requiresAuth : true}
    },
    {
        path:"/albumList",
        component:AlbumList,
        meta:{requiresAuth : true}
    },
    {
        path:"/albumAdd",
        component:AlbumAdd,
        meta:{requiresAuth : true}
    },
    {
        path:"/albumUpdate",
        component:AlbumUpdate,
        meta:{requiresAuth : true}
    },
    {
        path:"/episodeList",
        component:EpisodeList,
        meta:{requiresAuth : true}
    },
    {
        path:"/episodeAdd",
        component:EpisodeAdd,
        meta:{requiresAuth : true}
    },
    {
        path:"/episodeUpdate",
        component:EpisodeUpdate,
        meta:{requiresAuth : true}
    },
    {
        path:"/userAdd",
        component:UserAdd,
        meta:{requiresAuth : true}
    },
    {
        path:"/userList",
        component:UserList,
        meta:{requiresAuth : true}
    },
    {
        path:"/userUpdate",
        component:UserUpdate,
        meta:{requiresAuth : true}
    },
    // ===== 以下为游客页面，无需登录 =====
    {
        path:"/category",
        component:Category
    },
    {
        path:"/album",
        component:Album
    },
    {
        path:"/episode",
        component:Episode
    },
    {
        path:"/audio",
        component:AudioPlayer
    },
    // ===== 静态网站 =====
    {
        path:"/blogList",
        component:BlogList
    },
    {
        path:"/blogList/postDetail/:id",
        component:PostDetail
    },
    {
        path:"/resources",
        component:Resources
    },
    // ===== 信息页面 =====
    {
        path:"/info",
        component:InfoPage
    },
    // 旧路径重定向
    { path:"/about", redirect:"/info#about" },
    { path:"/contact", redirect:"/info#contact" },
    { path:"/disclaimer", redirect:"/info#legal" },
    { path:"/privacy", redirect:"/info#legal" }
]
const router = createRouter({
    history:createWebHistory(),
    routes:paths
})

router.beforeEach((to,from) =>{
    if(to.meta.requiresAuth){
        const token = localStorage.getItem("token")
        if(!token){
            return "/login"
        }
    }
})

export default router