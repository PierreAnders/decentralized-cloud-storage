<template>
    <div class="min-h-screen px-8 pt-8">
        <BurgerMenu />
        <div class="flex items-center justify-center pt-8">
            <h1 class="pr-3 tracking-wider text-light-gray">CODE</h1>
            <IconCode :color="'#334155'" />
        </div>
        <div class="mt-12">
            <div class="flex justify-end w-full mx-auto mt-4 md:w-10/12">
                <select v-model="selectedModel" class="w-36 px-2 py-0.5 text-xs bg-black border rounded-md text-light-gray border-light-gray">
                    <option v-for="model in models" :key="model.value" :value="model.value" class="text-xs">
                        {{ model.label }}
                    </option>
                </select>
            </div>
            <div v-for="message in messages" :key="message.id"
                :class="[message.role === 'user' ? 'bg-black' : 'bg-light-gray text-black', 'border mt-4 text-sm rounded-md mb-2 border-light-gray w-full mx-auto mt-4 md:w-10/12']">
                <pre><code class="rounded-md hljs" v-html="highlightCodeBlocks(message.content)"></code></pre>
            </div>
            <div class="flex items-center justify-between w-full mx-auto mt-4 md:w-10/12 input-container">
                <textarea v-model="userMessage" placeholder="Posez une question..."
                    class="flex-1 p-2 border-2 border-black rounded-md focus:outline-none focus:border-light-gray bg-blue-gray"></textarea>
            </div>
            <div class="flex justify-between w-full pb-24 mx-auto mt-3 mt-4 md:w-10/12 scrollbar-thin">
                <button @click="startSpeechRecognition">
                    <IconMicro class="transition-transform transform hover:scale-110" />
                </button>
                <button @click="sendMessage">
                    <div v-if="isLoading" class="spinner"></div>
                    <IconEnter v-if="!isLoading" class="transition-transform transform hover:scale-110" />
                </button>
            </div>
            <div id="speechOutput" class="mt-4 text-lg font-semibold"></div>
        </div>
    </div>
</template>

<script>
import axios from 'axios'
import hljs from 'highlight.js'
import 'highlight.js/styles/atom-one-dark.css'

const BASE_URL = import.meta.env.VITE_BASE_URL

export default {
    data() {
        return {
            messages: [],
            userMessage: '',
            isListening: false,
            jwtToken: null,
            isLoading: false,
            models: [
            { value: 'deepseek-r1:7b', label: 'DeepSeek R1' },
            { value: 'mistral', label: 'Mistral' },
            { value: 'llama3', label: 'LLaMA 3' },
            { value: 'codellama', label: 'Code Llama' },
            { value: 'qwen', label: 'Qwen' }
            ],
            selectedModel: 'deepseek-r1:7b'
        }
    },
    methods: {
        highlightCodeBlocks(text) {
            // Vérifiez si `text` est une chaîne de caractères
            if (typeof text !== 'string') {
                console.error('Expected a string but received:', text);
                return text; // Retournez la valeur telle quelle si ce n'est pas une chaîne
            }

            // Regex pour récupérer le contenu entre les blocs de code
            const regex = /```([^`]+)```/g;

            // Remplacer le contenu entre les blocs de code par le contenu mis en forme par highlight.js
            return text.replace(regex, (match, code) => {
                // Récupérer le code mis en forme par highlight.js
                const highlightedCode = hljs.highlightAuto(code).value;
                return `<pre><code>${highlightedCode}</code></pre>`;
            });
        },
        async sendMessage() {
            if (this.userMessage.trim() === '') return;

            this.jwtToken = localStorage.getItem('access_token')
            const sessionId = this.jwtToken.slice(0, 90)
            console.log('sessionId', sessionId)
            this.isLoading = true

            try {
                const response = await axios.post(`${BASE_URL}/api/Chat/message`, { 
                    message: this.userMessage,
                    model: this.selectedModel },
                    {
                headers: {
                    'Authorization': `Bearer ${this.jwtToken}`,
                    'Content-Type': 'application/json'
                }
                });

                const assistantReply = response.data.message.content;

                console.log('assistantReply', assistantReply)

                this.messages.push(
                    { role: 'user', content: this.userMessage },
                    { role: 'assistant', content: assistantReply }
                );

                const utterance = new SpeechSynthesisUtterance(assistantReply)
                const voices = speechSynthesis.getVoices()
                utterance.voice = voices[2]
                utterance.rate = 1
                speechSynthesis.speak(utterance)

                this.userMessage = ''

            } catch (error) {
                console.error(`Erreur lors de l'envoi de la requête : ${error}`)
            } finally {
                this.isLoading = false
            }
        },
        async startSpeechRecognition() {
            try {
                if ('webkitSpeechRecognition' in window) {
                    const recognition = new window.webkitSpeechRecognition()
                    recognition.lang = 'en-US'
                    recognition.onstart = () => {
                        this.isLoading = true
                    };
                    recognition.onresult = (event) => {
                        const speechResult = event.results[0][0].transcript
                        this.userMessage = speechResult
                        this.sendMessage()
                    };
                    recognition.onerror = (event) => {
                        console.error('Erreur de reconnaissance vocale', event.error)
                        this.isLoading = false
                    };
                    recognition.onend = () => {
                        this.isLoading = false
                    };
                    recognition.start()
                } else {
                    console.error('La reconnaissance vocale n\'est pas prise en charge dans ce navigateur.')
                }
            } catch (error) {
                console.error('Erreur lors de l\'initialisation de la reconnaissance vocale', error)
            }
        },
        setup() {
            definePageMeta({
                middleware: ['auth'],
            });
        },
    }
}
</script>