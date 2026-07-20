import { NgModule } from '@angular/core';
import { SharedModule } from '@shared/shared.module';
import { CommonModule } from '@angular/common';
import { ContractTemplatesRoutingModule } from './contract-templates-routing.module';
import { ContractTemplatesComponent } from './contract-templates.component';
import { CreateContractTemplateDialogComponent } from './create-contract-template/create-contract-template-dialog.component';
import { EditContractTemplateDialogComponent } from './edit-contract-template/edit-contract-template-dialog.component';

@NgModule({
    imports: [
        SharedModule,
        CommonModule,
        ContractTemplatesRoutingModule,
        ContractTemplatesComponent,
        CreateContractTemplateDialogComponent,
        EditContractTemplateDialogComponent,
    ],
})
export class ContractTemplatesModule {}
