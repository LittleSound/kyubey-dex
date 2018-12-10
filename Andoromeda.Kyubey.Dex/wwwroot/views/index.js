﻿app = new Vue({
    router: router,
    data: {
        chainId: 'aca376f206b8fc25a6ed44dbdc66547c36c6c33e3a119ffbeaef943642f0e906',
        host: 'nodes.get-scatter.com',
        account: null,
        uuid: null,
        loginMode: null,
        eos: null,
        requiredFields: null,
        volume: 0,
        lang: 'en',
        signalr: {
            simplewallet: {
                connection: null,
                listeners: []
            }
        },
    },
    created: function () {
        this.initSignalR();
        qv.get(`/api/v1/lang/${this.lang}/volume`, {}).then(res => {
            if (res.code === 200) {
                this.volume = res.data;
            }
        });
    },
    watch: {
    },
    methods: {
        getSimpleWalletUUID: function () {
            return this.uuid;
        },
        generateUUID: function () {
            var s = [];
            var hexDigits = "0123456789abcdef";
            for (var i = 0; i < 36; i++) {
                s[i] = hexDigits.substr(Math.floor(Math.random() * 0x10), 1);
            }
            s[14] = "4";
            s[19] = hexDigits.substr((s[19] & 0x3) | 0x8, 1);
            s[8] = s[13] = s[18] = s[23] = "-";

            var uuid = s.join("");
            return uuid;
        },
        initSignalR: function () {
            var self = this;
            self.signalr.simplewallet.connection = new signalR.HubConnectionBuilder()
                .configureLogging(signalR.LogLevel.Trace)
                .withUrl('/signalr/simplewallet', {})
                .withHubProtocol(new signalR.JsonHubProtocol())
                .build();

            // TODO: Receiving some signals for updating query view.

            self.signalr.simplewallet.connection.on('simpleWalletLoginSucceeded', (account) => {
                self.account = account;
                self.loginMode = 'Simple Wallet';
            });

            self.signalr.simplewallet.connection.start().then(function () {
                self.uuid = self.generateUUID();
                return self.signalr.simplewallet.connection.invoke('bindUUID', self.uuid);
            });

        },
        login: function () {
            $('#loginModal').modal('show');
        },
        scatterLogin: function () {
            if (!('scatter' in window)) {
                showModal('Scatter插件没有找到', 'Scatter是一款EOS钱包，运行在Chrome浏览器中，请您确定已经安装Scatter插件. 参考：https://www.jianshu.com/p/a2e1e6204023');
            } else {
                var self = this;
                var network = {
                    blockchain: 'eos',
                    host: self.host,
                    port: 443,
                    protocol: 'https',
                    chainId: self.chainId
                };
                scatter.getIdentity({ accounts: [network] }).then(identity => {
                    self.account = identity.accounts.find(acc => acc.blockchain === 'eos');
                    self.loginMode = 'Scatter Addons';
                    self.eos = scatter.eos(network, Eos, {});
                    self.requiredFields = { accounts: [network] };
                });
            }
            $('#loginModal').modal('hide');
        },
        scatterLogout: function () {
            var self = this;
            if (self.loginMode && (self.loginMode === 'Scatter Addons' || self.loginMode === 'Scatter Desktop')) {
                scatter.forgetIdentity()
                    .then(() => {
                        self.account = null;
                        self.loginMode = null;
                    });
            } else {
                self.account = null;
                self.loginMode = null;
            }
        },
        redirect: function (name, path, params, query) {
            if (name && !path)
                path = name;
            LazyRouting.RedirectTo(name, path, params, query);
        },
        setLang: function (param) { 
            this.$i18n.locale = param;
        },
        marked: function (md) {
            return marked(md); 
        }
    },
    computed: {
        isSignedIn: function () {
            return !!this.account;
        }
    },
    i18n: i18n
});