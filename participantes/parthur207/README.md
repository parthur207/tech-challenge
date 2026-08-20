# Entrega — parthur-207

## 1. Resumo da entrega

Back-end: O módulo de Beneficiários possuia apenas endpoints de Criação (post) e listagem (get) implementados, onde ambos possuiam gargalos reais.
Foi efetuado a correção desses defeitos, que consistia na ausencia de validações, ausencia de condicionais de verificação e interruptores de fluxos, possibilidade de concorrencia/gravação duplicada de atributos chaves, ausencia de paginação/filtros comnbináveis. Dito isso, o detalhamento das correções/implementações foram: validação real de um CPF (dígito
verificador + rejeição de sequência repetida), unicidade garantida por índice único no banco,
validação de plano (existente e não excluído), consultas por id, atualização com congelamento de
dados cadastrais para beneficiário com status (INATIVO), exclusão lógica, listagem paginada com filtros
combináveis e criação de DTOs seguindo o mesmo padrão de "Planos". 

Testes: Foi realizado mudanças e inserções de muitos testes afim de atingir maior abrangencia das funcionalidades do sistema. Incialmente, a ideia foi ler e entender na especificação citações sobre as regras de negócio para posteriormente replicar e cobrir tudo com os testes. Foi utilizado a IA com maior intensidade nos testes, onde optei pela prática SDD, visando detalhar com precisão as diretrizes e o fluxo das funcionalidades. 
Dentre os ajustes, o teste que antes permitia atualizar dados de um beneficiário inativo foi revertido, retornando (409: conflict) caso entrasse nessas condições. Adjunto, foi feito adição de testes para certificar que concorrencias em criação de beneficiários garantissem a unicidade de cpfs, assim como, para cobrir formatos erroneos dos cpfs. Foi feito tambem uma abrangencia que testava os métodos de domínio (métodos que criei dentro das entidades "beneficiario"/"plano") e testabilidade de operações que envolvessem um "Plano" com o atributo "ExcluidoEm". No mais, o foco se deu a testes ao backend, e a spec não relatou a necessidade de ter para o front.

Front-end: No front foi finalizado a parte de Beneficiários, sendo implementado as features de listagem, filtros, máscaras atreladas aos campos, paginação, formulário de cadastro/edição no mesmo padrão do bloco de planos. Quanto ao módulo dos planos, fiz pequenas correções, como o importação de DestroyRef para que durante o fluxo não ocorrer falhas durante carregamentos fora do construtor.

---

## 2. Decisões

### 2.1 Defeitos que encontrei no código base

- (POST /beneficiarios) devolvia 200 em vez de 201, sem redirecionamento do que foi criado. A tipagem (IActionResult) foi trocada para "CreatedAtAction", replicando como é em "PlanosController". Sem isso, o usupário não consegue visualizar com facilidade o recurso
criado.

- Validação de CPF era pouco abrangente, onde qualquer sequência de 11 dígitos passava. Movi a validação para o domínio "Beneficiario", com o
mesmo algoritmo do gerador de teste, mais a rejeição de sequência repetida. Sem essa correção,
CPFs inválidos ficariam cadastrados de forma permanente, quebrando qualquer integração que
dependa de CPF real.

- Sem unicidade real de CPF sob concorrência: A única proteção era uma certificação se o cpf ja existia no banco, sem índice único na tabela. Por conta disso, requisições simultâneas passavam pela checagem e concluíam a ação. Dito isso, a correção foi adicionar um índice único para o atributo "Cpf", além de inserir um captador de violação de unicidade. Foi coberto totalmente esse cenário com o teste (Criar_simultaneamente_com_mesmo_cpf_deve_garantir_unicidade) que dispara duas criações com o
mesmo CPF.

 - Plano inexistente ou excluído não era validado: Um "plano_id" inválido só estourava no
na tentativa de salva-lo ao banco. A correção envolveu adicionar uma função de auxilio que como seu nome ja diz, ela garantia que esse plano era válido "GarantirPlanoValidoAsync", usando tambem uma exeção personalizada que serviria perfeitamente para esse cenário "NaoProcessavelException". Por meio dessa correção, agora é feito a checagem e correspondencia dessa requisição com uma exceção mais direcionada.

- Tais fluxos não possuem condições que validam a anulabilidade do dado: Em determinados cenários, é realizado a query e posteriormente não se é verificado se o dado está preenchido, tornando possível uma exceção de referencia nula, ou a correspondencia da requisição diferente de 404 (NotFound); 

- GET /beneficiarios: Existia um gargalo de desempenho dentro do endpoint (N+1). O código resolvia o
plano de cada item com dentro de um foreach, consequentemente, eram feitas varias idas ao banco para consulta por item. A resposta agora expõe só plano_id. Nesse momento, são realizadas duas consultas por requisição para ser feito o builder do objeto de retorno e coleta da composição necessário para paginação dos dados.

- Front-End "planos-lista.ts" (Resolução com auxilio de IA): Em "planos-lista.ts", o método "carregar()"  chama
".pipe(takeUntilDestroyed())" sem argumento, e é disparado tanto no construtor quanto pelo
clique em "Recarregar". Essa forma só é válida dentro de um contexto de injeção, então clicar em "Recarregar" lançaria (NG0203) em runtime. A correção foi importar "DestroyRef" num campo e passando explicitamente (takeUntilDestroyed(this.destroyRef)), nos
três componentes.

- Front-End — Ausência de máscaras: Existe a possibilidade de inserção de dados incorretos diretamente pela UI, devido à ausência de validações no front-end. Essa limitação pode comprometer a integridade dos dados na camada de interface. A adoção de uma dupla validação (UI e API) reduz esse risco, permitindo que apenas requisições maliciosas ou fora do fluxo esperado cheguem diretamente à API. Além disso, o uso de máscaras reduz a ocorrência de entradas inválidas e, consequentemente, a quantidade de requisições desnecessárias ao servidor.

### 2.2 Pontos em que a especificação não definiu o comportamento

- Ordenação padrão da listagem: A seção 3 deixa a critério de quem implementa, sendo assim, a opção de ordenação foi mediante a data de cadastro: OrderBy(DataCadastro).ThenBy(Id).

- "Data passada" inclui hoje?": A seção 1 diz que "data_nascimento" precisa ser "data passada",
sem dizer a periodização, ou se hoje conta. Optei em tratar como estritamente anterior a hoje. É a leitura mais literal do
texto, embora a lista de erros da seção 2.3 fale só em "data no futuro" — se a intenção fosse
só bloquear datas futuras, aceitar o dia de hoje também seria defensável.

### 2.3 Inconsistências entre spec e testes

- Tamanho padrão de página: A SPEC diz 10 (seção 3), o teste público original esperava 20.
A decisão foi seguir conforme a SPEC, ajustando o tamanho da paginação para 10.

- Congelamento de "INATIVO": a seção 4.3 diz que alterar dado cadastral de um beneficiário
"INATIVO" deve devolver 409 ("registro congelado"). O teste público original fazia exatamente
essa alteração e esperava 200. Seguindo a SPEC, modifiquei o retorno do resultado nesses cenários.

### 2.4 Decisões técnicas

- Resposta de Beneficiário expõe só "plano_id", não o objeto "Plano" embutido, pois a funcionalidade não espera outros atributos além da chave do plano. Tal decisão resolve o N+1 do item acima e é o formato exato do exemplo da SPEC.
- Frontend sem router: o projeto já não usava nenhum, então a tela de Beneficiários alterna
  lista/formulário no mesmo componente via "signal", no mesmo estilo direto que "app.html" já
  usa para Planos.
- Não toquei na arquitetura nem na estrutura de pastas do Plano: Beneficiários segue as mesmas
  camadas (Dominio → Aplicacao → Api/Contratos → Controllers) e as mesmas pastas ja existentes.

### 2.5 O que ficou de fora

Testes automatizados de frontend: a seção 9.7 da SPEC diz que não entram na verificação
automática, e não havia nenhum já configurado no projeto.

---

## 3. Uso de IA

Usado para criar um ROADMAP simplificado e conciso dos requisitos/implementações esperados e correções a serem realizadas. Utilizei IA para gerar alguns testes, auxilio em decisão quanto a manter o padrão arquitetural pre-estabelecido e confirmação  
verificação end-to-end das funcionalidades.

### 3.1 Ferramentas

Além da utilização de IA como ferramenta de apoio à análise, planejamento e validação das funcionalidades, utilizei o MIRO durante a análise e implementação da solução. A ferramenta foi utilizada para desenhar diagramas, compreender visualmente o relacionamento entre as entidades e ilustrar o fluxo das requisições dentro do sistema, auxiliando na compreensão e replicação do padrão arquitetural já existente.

A utilização do MIRO contribuiu principalmente para o entendimento das relações entre Beneficiário, Plano, Domínio, Aplicação e API/Contratos, permitindo visualizar como uma requisição percorre as diferentes camadas antes de chegar à persistência dos dados. Esse mapeamento também foi utilizado como apoio para manter a implementação do módulo de Beneficiários compatível com o padrão previamente estabelecido no módulo de Planos.

- Segue o link: https://miro.com/app/board/uXjVHx6IJ4g=/?share_link_id=731888345002

### 3.2 Os 3 prompts que mais influenciaram o resultado

**Prompt 1**

```
Inspecione o readme desse repositorio git, faça um embasamento sobre todo o escopo de tarefas
a serem cumpridas e as liste para mim (ROADMAP). Posteriormente, faça um mapeamento ilustrativo sobre o padrão das requisições, para por meio disso, eu replicar o comportamento nas implementações esperadas sem fugir do modelo arquitetural. Adjunto, certifique e faça um análise crítica e traga uma conclusão se a refatoração do sistema para uma arquitetura moderna, criação de novas pastas, desacoplamento de funcionalidades, alteração da integridade das funcionalidades e principalmente, se a implementação de outros frameworks que visam enriquecer o projeto, como Redis, Swagger, SignalR ou mensageria seriam maléficos e fugiriam do solicitado.;
```

Foi esse prompt que definiu o guardrail do projeto inteiro: decidi não migrar para Clean
Architecture e não realizar mudanças brucas, porque a FAQ do "README.md" já desaconselha refatorar o módulo de Planos sem
motivo claro, podendo ser generalizado para o sistema em geral, e a avaliação não dá bônus por escopo além do pedido.

**Prompt 2**

```
Quais devem ser todos as diretrizes e certificações acopladas na verificação de um CPF, visando seu tamanho correto, padrão e etc. 

```

O embasamento das regras de negócio vinculadas ao cpf foi fundamental para implementar as regras de negócio como métodos de domínio na entidade "Beneficiario";. 

**Prompt 3**

```
Nesse momento, finalizei o teste end-to-end do sistema e vejo que ele atende aos critérios esperados. Dito isso, para batida de martelo, realize uma testabilidade em todas as funcionalidades, faça medições, seja imparcial, critério com o propósito de encontrar gargalos que poderiam impactar os pilares de segurança, escalabilidade e desempenho no sistema.
```

### 3.3 O que fiz sem IA

A análise de pertinencia das funcionalidades, assim como a inspeção de seu fluxo para ser encontrado gargalos, sejam eles de má práticas de modularização, problemas de desempenho e segurança foram feitos exclusivamente por mim. Tal prática me deu entendimento abrangente sobre o sistema e sobre seu fluxo, o que me auxilio em replicar esse padrão para o módulo de beneficiários. 
Isso envolveu a validação real de CPF, garantia de unicidade por índice no banco, tratamento de concorrência, validação de Planos existentes/não excluídos, correção do problema de N+1, paginação e filtros combináveis, congelamento de beneficiários INATIVO, exclusão lógica e criação dos DTOs seguindo o padrão de Planos.

### 3.4 O que ainda não domino

Meu principal ponto de evolução está no Angular, especificamente em conceitos e recursos mais avançados do framework. Possuo domínio de desenvolvimento front-end com React, porém ainda estou consolidando minha experiência com Angular e seus mecanismos próprios. Como exemplo, o uso de DestroyRef em conjunto com takeUntilDestroyed() foi um conceito em que precisei de apoio da IA para compreender o contexto de injeção e o gerenciamento do ciclo de vida dos componentes. Também pretendo aprofundar conhecimentos em ciclo de vida do Angular, gerenciamento de subscriptions, estado e testes automatizados de front-end.

---

## 4. Perguntas de compreensão

### 4.1 Concorrência

A pré-checagem em "GarantirCpfUnicoAsync" não garante unicidade sozinha: duas requisições podem passar por ela antes de qualquer uma
commitar. Quem garante de verdade é o índice único "IX_Beneficiarios_Cpf", aplicado pela migration "AdicionaExclusaoLogicaBeneficiarios". Quando as
duas transações tentam inserir o mesmo CPF, o Postgres deixa uma passar e rejeita a outra. O "BeneficiarioServico.SalvarAsync" captura uma exceção e converte
em "ConflitoException", técnica tambem presente em "PlanoServico" já usava para
"nome"/"codigo_registro_ans". O teste "Criar_simultaneamente_com_mesmo_cpf_deve_garantir_unicidade"
dispara as duas criações com "Task.WhenAll" e confirma exatamente um 201 e um 409.

### 4.2 Um defeito que você corrigiu

Em "BeneficiariosController.Listar", o comentário garantia que resolver o plano de cada beneficiário com uma lambda de "find" passando o id do plano em um foreach continuava sendo
"uma única ida ao banco, qualquer que seja o tamanho da página", mas não era a realidade: o "FindAsync" realizava muitas consultas, e a consulta principal já trazia a tabela inteira, sem paginação nenhuma. O código fazia o oposto do que o comentário dizia. Em produção, uma listagem com centenas de beneficiários geraria centenas de consultas extras, impactando diretamente o desempenho. A Correção foi tirar o relacionamento entre beneficiário e plano (a resposta expõe só "plano_id") e remover o foreach e as idas ao banco dentro do mesmo (N+1 queries), então "ListarAsync" passou a realizar duas requisições ao banco e com paginação fixa ou não.

### 4.3 O trecho mais complexo

```csharp
private static int CalcularDigitoVerificador(IReadOnlyList<int> digitos, int pesoInicial)
{
    var soma = 0;
    for (var i = 0; i < pesoInicial - 1; i++)
    {
        soma += digitos[i] * (pesoInicial - i);
    }
    var resto = soma * 10 % 11;
    return resto == 10 ? 0 : resto;
}
```
Considerei esse o trecho mais complexo porque, apesar de possuir poucas linhas, ele concentra uma regra de negócio importante e exige bastante atenção à lógica do algoritmo de validação do CPF. O cálculo depende da quantidade de dígitos considerados, da aplicaçao correta dos pesos em ordem decrescente e da utilização da regra de módulo 11.
Além disso, o mesmo método é reutilizado para calcular os dois dígitos verificadores, alterando apenas o pesoInicial. Um pequeno erro na quantidade de iterações, nos pesos ou no tratamento do resto 10 poderia fazer com que CPFs válidos fossem rejeitados ou CPFs inválidos fossem aceitos.
Por esse motivo, considerei esse trecho mais complexo principalmente pela necessidade de compreender e implementar corretamente a regra, e não pelo tamanho do código em si.